using Microsoft.Maui.Controls.Shapes;
using TischplanApp.Models;

namespace TischplanApp.Controls;

/// <summary>
/// A custom control that provides zoom and pan functionality for a canvas with table elements.
/// Uses simple, direct gesture handling for maximum smoothness.
/// </summary>
public class ZoomPanCanvas : ContentView
{
    private const double MinScale = 0.1;
    private const double MaxScale = 3.0;

    private readonly Grid _rootGrid;
    private readonly ContentView _contentHost;
    private readonly AbsoluteLayout _canvas;

    private double _currentScale = 1.0;
    private double _startScale = 1.0;
    private double _xOffset = 0.0;
    private double _yOffset = 0.0;
    private bool _isInitialized = false;

    // Public property for Android Handler to access
    public ContentView ContentHost => _contentHost;

    public ZoomPanCanvas()
    {
        // Create the canvas
        _canvas = new AbsoluteLayout
        {
            WidthRequest = 3000,
            HeightRequest = 2000,
            BackgroundColor = Colors.White
        };

        // Content host that will be transformed
        _contentHost = new ContentView
        {
            Content = _canvas,
            AnchorX = 0.5,  // Zentriert
            AnchorY = 0.5,  // Zentriert
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };

        // Root grid
        _rootGrid = new Grid
        {
            BackgroundColor = Colors.LightGray,
            Children = { _contentHost }
        };

        Content = _rootGrid;

#if WINDOWS
        // On Windows, use MAUI gesture recognizers
        // On Android/iOS, we use native gesture recognizers via Custom Handlers
        var pinchGesture = new PinchGestureRecognizer();
        pinchGesture.PinchUpdated += OnPinchUpdated;
        GestureRecognizers.Add(pinchGesture);

        var panGesture = new PanGestureRecognizer();
        panGesture.PanUpdated += OnPanUpdated;
        GestureRecognizers.Add(panGesture);
#endif

        // Mouse wheel support for Windows
        this.HandlerChanged += OnHandlerChanged;

        // Initialize center position when size is known
        this.SizeChanged += OnSizeChanged;
    }

    private void OnSizeChanged(object? sender, EventArgs e)
    {
        if (!_isInitialized && Width > 0 && Height > 0)
        {
            // Zentriere den Canvas initial
            _xOffset = 0;
            _yOffset = 0;
            _contentHost.TranslationX = _xOffset;
            _contentHost.TranslationY = _yOffset;
            _isInitialized = true;
        }
    }

    private void OnHandlerChanged(object? sender, EventArgs e)
    {
        if (this.Handler?.PlatformView != null)
        {
#if WINDOWS
            var platformView = this.Handler.PlatformView as Microsoft.UI.Xaml.FrameworkElement;
            if (platformView != null)
            {
                platformView.PointerWheelChanged += OnPointerWheelChanged;
            }
#endif
        }
    }

#if WINDOWS
    private void OnPointerWheelChanged(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        var pointerPoint = e.GetCurrentPoint(sender as Microsoft.UI.Xaml.UIElement);
        var delta = pointerPoint.Properties.MouseWheelDelta;
        var zoomFactor = delta > 0 ? 1.1 : 0.9;
        var oldScale = _currentScale;
        var newScale = oldScale * zoomFactor;
        newScale = Math.Max(MinScale, Math.Min(MaxScale, newScale));

        if (Math.Abs(newScale - oldScale) > 0.001)
        {
            // Translation proportional skalieren - was du siehst bleibt gleich!
            var scaleRatio = newScale / oldScale;
            _xOffset *= scaleRatio;
            _yOffset *= scaleRatio;
            _currentScale = newScale;
            _contentHost.Scale = _currentScale;
            _contentHost.TranslationX = _xOffset;
            _contentHost.TranslationY = _yOffset;
        }
        e.Handled = true;
    }
#endif

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
            Content = border
        };

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

    private void OnPinchUpdated(object? sender, PinchGestureUpdatedEventArgs e)
    {
        if (e.Status == GestureStatus.Started)
        {
            _startScale = _currentScale;
        }
        else if (e.Status == GestureStatus.Running)
        {
            // Direct scale calculation
            var oldScale = _currentScale;
            var newScale = _startScale * e.Scale;
            newScale = Math.Max(MinScale, Math.Min(MaxScale, newScale));

            if (Math.Abs(newScale - oldScale) < 0.001) return;

            // Translation proportional skalieren - was du siehst bleibt gleich!
            var scaleRatio = newScale / oldScale;
            _xOffset *= scaleRatio;
            _yOffset *= scaleRatio;
            _currentScale = newScale;

            // Update immediately
            _contentHost.Scale = _currentScale;
            _contentHost.TranslationX = _xOffset;
            _contentHost.TranslationY = _yOffset;
        }
    }

    private double _lastPanX = 0;
    private double _lastPanY = 0;

    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _lastPanX = 0;
                _lastPanY = 0;
                break;

            case GestureStatus.Running:
                var deltaX = e.TotalX - _lastPanX;
                var deltaY = e.TotalY - _lastPanY;
                _xOffset += deltaX;
                _yOffset += deltaY;
                _lastPanX = e.TotalX;
                _lastPanY = e.TotalY;
                _contentHost.TranslationX = _xOffset;
                _contentHost.TranslationY = _yOffset;
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                _lastPanX = 0;
                _lastPanY = 0;
                break;
        }
    }

    public void ResetZoomPan()
    {
        _currentScale = 1.0;
        _startScale = 1.0;
        _xOffset = 0.0;
        _yOffset = 0.0;
        _contentHost.Scale = 1.0;
        _contentHost.TranslationX = 0.0;
        _contentHost.TranslationY = 0.0;
    }
}
