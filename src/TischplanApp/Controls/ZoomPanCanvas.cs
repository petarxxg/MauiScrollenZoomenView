using Microsoft.Maui.Controls.Shapes;
using TischplanApp.Models;
using MRGestures = MR.Gestures;

namespace TischplanApp.Controls;

/// <summary>
/// A custom control that provides zoom and pan functionality for a canvas with table elements.
/// Uses MR.Gestures for smooth, professional zoom and pan on Android, iOS and Windows.
/// </summary>
public class ZoomPanCanvas : Microsoft.Maui.Controls.ContentView
{
    private const double MinScale = 0.1;   // 10% - viel weiter rauszoomen möglich
    private const double MaxScale = 3.0;   // 300% - maximaler Zoom

    private readonly Grid _rootGrid;
    private readonly MRGestures.ContentView _contentHost;
    private readonly AbsoluteLayout _canvas;

    private double _currentScale = 1.0;
    private double _xOffset = 0.0;
    private double _yOffset = 0.0;

    public ZoomPanCanvas()
    {
        // Create the canvas with fixed size
        _canvas = new AbsoluteLayout
        {
            WidthRequest = 3000,
            HeightRequest = 2000,
            BackgroundColor = Colors.White
        };

        // MR.Gestures ContentView - supports smooth gestures
        _contentHost = new MRGestures.ContentView
        {
            Content = _canvas,
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

        // Setup MR.Gestures events
        _contentHost.Pinching += OnPinching;
        _contentHost.Panning += OnPanning;

        // Add mouse wheel support for desktop platforms
        this.HandlerChanged += OnHandlerChanged;
    }

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

            // Apply transformations
            _contentHost.Scale = _currentScale;
            _contentHost.TranslationX = _xOffset;
            _contentHost.TranslationY = _yOffset;
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
    private Microsoft.Maui.Controls.ContentView CreateTableView(TableModel table)
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

        var tableView = new Microsoft.Maui.Controls.ContentView
        {
            Content = border,
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
    /// Handles pinch gesture for zooming using MR.Gestures.
    /// </summary>
    private void OnPinching(object? sender, MRGestures.PinchEventArgs e)
    {
        if (e.DeltaScale == 0) return;

        // Calculate new scale based on cumulative scale
        var newScale = _currentScale * e.DeltaScale;
        newScale = Math.Max(MinScale, Math.Min(MaxScale, newScale));

        if (Math.Abs(newScale - _currentScale) < 0.001) return;

        // Get pinch center in screen coordinates
        var pinchCenterX = e.Center.X;
        var pinchCenterY = e.Center.Y;

        // Calculate content position before scaling
        var contentX = (pinchCenterX - _xOffset) / _currentScale;
        var contentY = (pinchCenterY - _yOffset) / _currentScale;

        // Update scale
        _currentScale = newScale;

        // Adjust offset so pinch point stays fixed
        _xOffset = pinchCenterX - (contentX * _currentScale);
        _yOffset = pinchCenterY - (contentY * _currentScale);

        // Apply transformations
        _contentHost.Scale = _currentScale;
        _contentHost.TranslationX = _xOffset;
        _contentHost.TranslationY = _yOffset;
    }

    /// <summary>
    /// Handles pan gesture for panning using MR.Gestures.
    /// </summary>
    private void OnPanning(object? sender, MRGestures.PanEventArgs e)
    {
        // MR.Gestures provides delta values directly
        _xOffset += e.DeltaDistance.X;
        _yOffset += e.DeltaDistance.Y;

        // Apply translation
        _contentHost.TranslationX = _xOffset;
        _contentHost.TranslationY = _yOffset;
    }

    /// <summary>
    /// Public method to reset zoom and pan to initial state.
    /// </summary>
    public void ResetZoomPan()
    {
        _currentScale = 1.0;
        _xOffset = 0.0;
        _yOffset = 0.0;

        _contentHost.Scale = 1.0;
        _contentHost.TranslationX = 0.0;
        _contentHost.TranslationY = 0.0;
    }
}
