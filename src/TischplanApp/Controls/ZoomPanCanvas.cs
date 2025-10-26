using Microsoft.Maui.Controls.Shapes;
using TischplanApp.Models;

namespace TischplanApp.Controls;

/// <summary>
/// A custom control that provides zoom and pan functionality for a canvas with table elements.
/// Uses PinchGestureRecognizer for zooming and PanGestureRecognizer for panning.
/// </summary>
public class ZoomPanCanvas : ContentView
{
    private const double MinScale = 0.5;
    private const double MaxScale = 3.0;

    private readonly Grid _rootGrid;
    private readonly ContentView _contentHost;
    private readonly AbsoluteLayout _canvas;

    private double _currentScale = 1.0;
    private double _startScale = 1.0;
    private double _xOffset = 0.0;
    private double _yOffset = 0.0;

    // Performance optimization: throttle updates
    private DateTime _lastUpdateTime = DateTime.MinValue;
    private const int UpdateThrottleMs = 16; // ~60 FPS max

    public ZoomPanCanvas()
    {
        // Create the canvas with fixed size
        _canvas = new AbsoluteLayout
        {
            WidthRequest = 3000,
            HeightRequest = 2000,
            BackgroundColor = Colors.White
        };

        // Content host that will be scaled and translated
        _contentHost = new ContentView
        {
            Content = _canvas,
            AnchorX = 0,
            AnchorY = 0,
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Start
        };

        // Root grid with clipping
        _rootGrid = new Grid
        {
            BackgroundColor = Colors.LightGray,
            Children = { _contentHost }
        };

        Content = _rootGrid;

        // Enable hardware acceleration for better performance
        _contentHost.IsClippedToBounds = false;

        // Add gesture recognizers
        var pinchGesture = new PinchGestureRecognizer();
        pinchGesture.PinchUpdated += OnPinchUpdated;
        GestureRecognizers.Add(pinchGesture);

        var panGesture = new PanGestureRecognizer();
        panGesture.PanUpdated += OnPanUpdated;
        GestureRecognizers.Add(panGesture);

        // Add mouse wheel support for desktop platforms
        this.HandlerChanged += OnHandlerChanged;
    }

    private double _lastPanX = 0;
    private double _lastPanY = 0;

    /// <summary>
    /// Handles the handler changed event to set up platform-specific mouse wheel support.
    /// </summary>
    private void OnHandlerChanged(object? sender, EventArgs e)
    {
        if (this.Handler?.PlatformView != null)
        {
#if WINDOWS
            // Add Windows-specific mouse wheel handling
            var platformView = this.Handler.PlatformView as Microsoft.UI.Xaml.FrameworkElement;
            if (platformView != null)
            {
                platformView.PointerWheelChanged += OnPointerWheelChanged;
            }
#endif
        }
    }

#if WINDOWS
    /// <summary>
    /// Handles mouse wheel events on Windows for zooming.
    /// </summary>
    private void OnPointerWheelChanged(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        var pointerPoint = e.GetCurrentPoint(sender as Microsoft.UI.Xaml.UIElement);
        var delta = pointerPoint.Properties.MouseWheelDelta;

        // Calculate zoom factor (positive delta = zoom in, negative = zoom out)
        var zoomFactor = delta > 0 ? 1.1 : 0.9;

        // Calculate new scale
        var newScale = _currentScale * zoomFactor;
        newScale = Math.Max(MinScale, Math.Min(MaxScale, newScale));

        if (Math.Abs(newScale - _currentScale) > 0.001)
        {
            // Get mouse position relative to this control
            var mouseX = pointerPoint.Position.X;
            var mouseY = pointerPoint.Position.Y;

            // Calculate the point in content coordinates before scaling
            var contentX = (mouseX - _xOffset) / _currentScale;
            var contentY = (mouseY - _yOffset) / _currentScale;

            // Update scale
            _currentScale = newScale;

            // Adjust offset so the mouse position remains at the same screen position
            _xOffset = mouseX - (contentX * _currentScale);
            _yOffset = mouseY - (contentY * _currentScale);

            ApplyTransformation();
        }

        e.Handled = true;
    }
#endif

    /// <summary>
    /// Loads table data and creates visual elements for each table.
    /// </summary>
    public void LoadTables(IEnumerable<TableModel> tables)
    {
        _canvas.Children.Clear();

        foreach (var table in tables)
        {
            var tableView = CreateTableView(table);
            AbsoluteLayout.SetLayoutBounds(tableView, new Rect(table.X, table.Y, table.Width, table.Height));
            _canvas.Children.Add(tableView);
        }
    }

    /// <summary>
    /// Creates a visual representation of a table.
    /// </summary>
    private ContentView CreateTableView(TableModel table)
    {
        var border = new Border
        {
            Stroke = Colors.DarkBlue,
            StrokeThickness = 2,
            BackgroundColor = Colors.LightBlue,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Content = new Label
            {
                Text = table.Name,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.DarkBlue
            }
        };

        var tableView = new ContentView
        {
            Content = border,
            // Enable hardware acceleration for smoother rendering
            IsClippedToBounds = false
        };

        // Add tap gesture
        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += async (s, e) =>
        {
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Tisch Info",
                    $"Sie haben {table.Name} ausgewählt.\nPosition: ({table.X:F0}, {table.Y:F0})",
                    "OK").ConfigureAwait(false);
            }
        };
        tableView.GestureRecognizers.Add(tapGesture);

        return tableView;
    }

    /// <summary>
    /// Handles pinch gesture for zooming.
    /// </summary>
    private void OnPinchUpdated(object? sender, PinchGestureUpdatedEventArgs e)
    {
        if (e.Status == GestureStatus.Started)
        {
            _startScale = _currentScale;
            _lastUpdateTime = DateTime.MinValue; // Reset throttle
            SetHardwareLayer(true); // Enable hardware layer for performance
        }
        else if (e.Status == GestureStatus.Running)
        {
            // Throttle updates for better performance
            var now = DateTime.UtcNow;
            if ((now - _lastUpdateTime).TotalMilliseconds < UpdateThrottleMs)
            {
                return; // Skip this frame
            }
            _lastUpdateTime = now;

            // Calculate new scale
            var newScale = _startScale * e.Scale;
            newScale = Math.Max(MinScale, Math.Min(MaxScale, newScale));

            // Calculate the pinch center point in screen coordinates
            var pinchCenterX = e.ScaleOrigin.X * Width;
            var pinchCenterY = e.ScaleOrigin.Y * Height;

            // Calculate the point in content coordinates before scaling
            var contentX = (pinchCenterX - _xOffset) / _currentScale;
            var contentY = (pinchCenterY - _yOffset) / _currentScale;

            // Update scale
            _currentScale = newScale;

            // Adjust offset so the pinch center remains at the same screen position
            _xOffset = pinchCenterX - (contentX * _currentScale);
            _yOffset = pinchCenterY - (contentY * _currentScale);

            ApplyTransformation();
        }
        else if (e.Status == GestureStatus.Completed || e.Status == GestureStatus.Canceled)
        {
            SetHardwareLayer(false); // Disable hardware layer after gesture
        }
    }

    /// <summary>
    /// Handles pan gesture for panning.
    /// </summary>
    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _lastPanX = 0;
                _lastPanY = 0;
                _lastUpdateTime = DateTime.MinValue;
                SetHardwareLayer(true);
                break;

            case GestureStatus.Running:
                // Throttle updates for better performance
                var now = DateTime.UtcNow;
                if ((now - _lastUpdateTime).TotalMilliseconds < UpdateThrottleMs)
                {
                    return; // Skip this frame
                }
                _lastUpdateTime = now;

                var deltaX = e.TotalX - _lastPanX;
                var deltaY = e.TotalY - _lastPanY;

                _xOffset += deltaX;
                _yOffset += deltaY;

                _lastPanX = e.TotalX;
                _lastPanY = e.TotalY;

                ApplyTransformation();
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                _lastPanX = 0;
                _lastPanY = 0;
                SetHardwareLayer(false);
                break;
        }
    }

    /// <summary>
    /// Applies the current scale and translation to the content host.
    /// </summary>
    private void ApplyTransformation()
    {
        // Direct property updates - batching can cause lag
        _contentHost.Scale = _currentScale;
        _contentHost.TranslationX = _xOffset;
        _contentHost.TranslationY = _yOffset;
    }

    /// <summary>
    /// Sets hardware layer for better performance during gestures (Android only).
    /// </summary>
    private void SetHardwareLayer(bool enable)
    {
#if ANDROID
        if (this.Handler?.PlatformView is Android.Views.View androidView)
        {
            androidView.SetLayerType(
                enable ? Android.Views.LayerType.Hardware : Android.Views.LayerType.None,
                null);
        }
#endif
    }

    /// <summary>
    /// Public method to reset zoom and pan to initial state.
    /// </summary>
    public void ResetZoomPan()
    {
        _currentScale = 1.0;
        _startScale = 1.0;
        _xOffset = 0.0;
        _yOffset = 0.0;
        ApplyTransformation();
    }
}
