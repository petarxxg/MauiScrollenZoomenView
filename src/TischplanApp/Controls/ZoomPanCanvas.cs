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

    // Public properties to get content dimensions for boundary checking
    public double ContentWidth => _canvas.WidthRequest;
    public double ContentHeight => _canvas.HeightRequest;

    // BindableProperty for Tables (now uses PositionableItem base class)
    public static readonly BindableProperty TablesProperty = BindableProperty.Create(
        nameof(Tables),
        typeof(IEnumerable<PositionableItem>),
        typeof(ZoomPanCanvas),
        default(IEnumerable<PositionableItem>),
        propertyChanged: OnTablesPropertyChanged);

    public IEnumerable<PositionableItem>? Tables
    {
        get => (IEnumerable<PositionableItem>?)GetValue(TablesProperty);
        set => SetValue(TablesProperty, value);
    }

    private static void OnTablesPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is ZoomPanCanvas canvas && newValue is IEnumerable<PositionableItem> tables)
        {
            canvas.LoadTables(tables);
        }
    }

    // BindableProperty for ItemTemplate
    public static readonly BindableProperty ItemTemplateProperty = BindableProperty.Create(
        nameof(ItemTemplate),
        typeof(DataTemplate),
        typeof(ZoomPanCanvas),
        default(DataTemplate),
        propertyChanged: OnItemTemplatePropertyChanged);

    public DataTemplate? ItemTemplate
    {
        get => (DataTemplate?)GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    private static void OnItemTemplatePropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is ZoomPanCanvas canvas && canvas.Tables != null)
        {
            // Reload tables wenn sich das Template ändert
            canvas.LoadTables(canvas.Tables);
        }
    }

    // BindableProperty for ItemTemplateSelector
    public static readonly BindableProperty ItemTemplateSelectorProperty = BindableProperty.Create(
        nameof(ItemTemplateSelector),
        typeof(DataTemplateSelector),
        typeof(ZoomPanCanvas),
        default(DataTemplateSelector),
        propertyChanged: OnItemTemplateSelectorPropertyChanged);

    public DataTemplateSelector? ItemTemplateSelector
    {
        get => (DataTemplateSelector?)GetValue(ItemTemplateSelectorProperty);
        set => SetValue(ItemTemplateSelectorProperty, value);
    }

    private static void OnItemTemplateSelectorPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is ZoomPanCanvas canvas && canvas.Tables != null)
        {
            // Reload tables wenn sich der Selector ändert
            canvas.LoadTables(canvas.Tables);
        }
    }

    public ZoomPanCanvas()
    {
        // Create the canvas
        _canvas = new AbsoluteLayout
        {
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

            // Clamp translation to prevent scrolling outside content bounds
            ClampTranslation();

            _contentHost.Scale = _currentScale;
            _contentHost.TranslationX = _xOffset;
            _contentHost.TranslationY = _yOffset;
        }
        e.Handled = true;
    }
#endif

    public void LoadTables(IEnumerable<PositionableItem> tables)
    {
        _canvas.Children.Clear();

        if (!tables.Any())
        {
            return;
        }

        // Finde die maximalen Koordinaten um die Canvas-Größe zu berechnen
        double maxX = 0;
        double maxY = 0;

        foreach (var item in tables)
        {
            var itemView = CreateItemView(item);
            AbsoluteLayout.SetLayoutBounds(itemView, new Rect(item.X, item.Y, item.Width, item.Height));
            _canvas.Children.Add(itemView);

            // Berechne die maximalen Koordinaten (Position + Größe)
            maxX = Math.Max(maxX, item.X + item.Width);
            maxY = Math.Max(maxY, item.Y + item.Height);
        }

        // Setze die Canvas-Größe mit etwas Padding (z.B. 50 Pixel Rand)
        const double padding = 50;
        _canvas.WidthRequest = maxX + padding;
        _canvas.HeightRequest = maxY + padding;
    }

    private ContentView CreateItemView(PositionableItem item)
    {
        ContentView itemView;
        DataTemplate? template = null;

        // Priorität: ItemTemplateSelector > ItemTemplate > Default
        if (ItemTemplateSelector != null)
        {
            template = ItemTemplateSelector.SelectTemplate(item, this);
        }
        else if (ItemTemplate != null)
        {
            template = ItemTemplate;
        }

        // Verwende Template wenn vorhanden
        if (template != null)
        {
            var content = template.CreateContent();

            if (content is View view)
            {
                itemView = new ContentView
                {
                    Content = view
                };
            }
            else if (content is ViewCell viewCell)
            {
                itemView = new ContentView
                {
                    Content = viewCell.View
                };
            }
            else
            {
                throw new InvalidOperationException("Template must create a View or ViewCell");
            }

            // Setze BindingContext auf das Item
            itemView.BindingContext = item;
        }
        else
        {
            throw new ArgumentNullException();
        }

        return itemView;
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

            // Clamp translation to prevent scrolling outside content bounds
            ClampTranslation();

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

                // Clamp translation to prevent scrolling outside content bounds
                ClampTranslation();

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

    private void ClampTranslation()
    {
        // Get content and viewport dimensions
        var contentWidth = _canvas.WidthRequest;
        var contentHeight = _canvas.HeightRequest;
        var viewportWidth = Width;
        var viewportHeight = Height;

        if (contentWidth <= 0 || contentHeight <= 0 || viewportWidth <= 0 || viewportHeight <= 0)
            return;

        // Calculate scaled content dimensions
        var scaledWidth = contentWidth * _currentScale;
        var scaledHeight = contentHeight * _currentScale;

        // Calculate maximum allowed translation
        // With AnchorX/Y = 0.5, the content is centered, so we need to account for that
        var maxTranslateX = Math.Max(0, (scaledWidth - viewportWidth) / 2);
        var maxTranslateY = Math.Max(0, (scaledHeight - viewportHeight) / 2);

        // Clamp translation
        _xOffset = Math.Max(-maxTranslateX, Math.Min(maxTranslateX, _xOffset));
        _yOffset = Math.Max(-maxTranslateY, Math.Min(maxTranslateY, _yOffset));
    }
}
