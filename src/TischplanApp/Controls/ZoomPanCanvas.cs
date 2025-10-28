

using Orderlyze.Foundation.Helper.Extensions;
using Orderlyze.Foundation.Interfaces;

namespace SharedControlsModule.PlatformControls;

/// <summary>
/// A custom control that provides zoom and pan functionality for a canvas with table elements.
/// Uses simple, direct gesture handling for maximum smoothness.
/// </summary>
public class ZoomPanCanvas : ContentView
{
    public const double MinScale = 0.4;
    public const double MaxScale = 1.0;

    private readonly Grid _rootGrid;
    private readonly ContentView _contentHost;
    private readonly AbsoluteLayout _canvas;

    private double _currentScale = 1.0;
    private double _startScale = 1.0;
    private double _xOffset = 0.0;
    private double _yOffset = 0.0;
    private bool _isInitialized = false;
    private bool _isPinching = false;

    // Public property for Android Handler to access
    public ContentView ContentHost => _contentHost;

    // Public properties to get content dimensions for boundary checking
    public double ContentWidth => _canvas.WidthRequest;
    public double ContentHeight => _canvas.HeightRequest;

    // BindableProperty for Tables (now uses IPositionBase base class)
    public static readonly BindableProperty TablesProperty = BindableProperty.Create(
        nameof(Tables),
        typeof(IEnumerable<IPositionBase>),
        typeof(ZoomPanCanvas),
        default(IEnumerable<IPositionBase>),
        propertyChanged: OnTablesPropertyChanged);

    public IEnumerable<IPositionBase>? Tables
    {
        get => (IEnumerable<IPositionBase>?)GetValue(TablesProperty);
        set => SetValue(TablesProperty, value);
    }

    private static void OnTablesPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is ZoomPanCanvas canvas && newValue is IEnumerable<IPositionBase> tables)
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

    // BindableProperty for Zoom
    public static readonly BindableProperty ZoomProperty = BindableProperty.Create(
        nameof(Zoom),
        typeof(double),
        typeof(ZoomPanCanvas),
        1.0,
        BindingMode.TwoWay,
        propertyChanged: OnScalePropertyChanged);

    public double Zoom
    {
        get => (double)GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, value);
    }

    private static void OnScalePropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is ZoomPanCanvas canvas && newValue is double scale)
        {
            canvas.SetScale(scale);
        }
    }

    // BindableProperty for IsEditMode
    public static readonly BindableProperty IsEditModeProperty = BindableProperty.Create(
        nameof(IsEditMode),
        typeof(bool),
        typeof(ZoomPanCanvas),
        false,
        propertyChanged: OnIsEditModePropertyChanged);

    public bool IsEditMode
    {
        get => (bool)GetValue(IsEditModeProperty);
        set => SetValue(IsEditModeProperty, value);
    }

    private static void OnIsEditModePropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is ZoomPanCanvas canvas && canvas.Tables != null)
        {
            // Reload tables to apply/remove edit mode gestures
            canvas.LoadTables(canvas.Tables);
        }
    }

    // BindableProperty for ItemTappedCommand
    public static readonly BindableProperty ItemTappedCommandProperty = BindableProperty.Create(
        nameof(ItemTappedCommand),
        typeof(System.Windows.Input.ICommand),
        typeof(ZoomPanCanvas),
        null);

    public System.Windows.Input.ICommand? ItemTappedCommand
    {
        get => (System.Windows.Input.ICommand?)GetValue(ItemTappedCommandProperty);
        set => SetValue(ItemTappedCommandProperty, value);
    }

    private void SetScale(double scale)
    {
        // Clamp scale
        scale = Math.Max(MinScale, Math.Min(MaxScale, scale));

        if (Math.Abs(_currentScale - scale) < 0.001)
            return;

        _currentScale = scale;
        _startScale = scale;

        // Apply to UI
        _contentHost.Scale = _currentScale;

        // Clamp translation after scale change
        ClampTranslation();
        _contentHost.TranslationX = _xOffset;
        _contentHost.TranslationY = _yOffset;

        // Update the property without triggering the changed event again
        if (Math.Abs(Zoom - scale) > 0.001)
        {
            SetValue(ZoomProperty, scale);
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
            AnchorX = 0,  // Links
            AnchorY = 0,  // Oben
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Start
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
            // Bei Anchor 0,0 (top-left): Zoom erfolgt von oben links
            _currentScale = newScale;

            // Update scale
            _contentHost.Scale = _currentScale;

            // Clamp translation after zoom
            ClampTranslation();
            _contentHost.TranslationX = _xOffset;
            _contentHost.TranslationY = _yOffset;

            // Update BindableProperty
            SetValue(ScaleProperty, _currentScale);
        }
        e.Handled = true;
    }
#endif

    public void LoadTables(IEnumerable<IPositionBase> tables)
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
            AbsoluteLayout.SetLayoutBounds(itemView, new Rect(item.Xposition.ToDouble(), item.Yposition.ToDouble(), item.Width.ToDouble(), item.Height.ToDouble()));
            _canvas.Children.Add(itemView);

            // Berechne die maximalen Koordinaten (Position + Größe)
            maxX = Math.Max(maxX, (item.Xposition + item.Width).ToDouble());
            maxY = Math.Max(maxY, (item.Yposition + item.Height).ToDouble());
        }

        // Setze die Canvas-Größe mit etwas Padding (z.B. 50 Pixel Rand)
        const double padding = 50;
        _canvas.WidthRequest = maxX + padding;
        _canvas.HeightRequest = maxY + padding;
    }

    private ContentView CreateItemView(IPositionBase item)
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
            else
            {
                throw new InvalidOperationException("Template must create a View or ViewCell");
            }

            // Setze BindingContext auf das Item
            itemView.BindingContext = item;

            // Add tap gesture for item tapped command
            if (ItemTappedCommand != null)
            {
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += (s, e) => OnItemTapped(item);
                itemView.GestureRecognizers.Add(tapGesture);
            }

            // Add edit mode gestures if enabled
            if (IsEditMode)
            {
                // Drag gesture for moving items
                var panGesture = new CorrectedPanGestureRecognizer();
                panGesture.PanUpdated += (s, e) => OnItemPanUpdated(s, e, item, itemView);
                itemView.GestureRecognizers.Add(panGesture);
            }
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
            _isPinching = true;
            _startScale = _currentScale;
        }
        else if (e.Status == GestureStatus.Running)
        {
            // Direct scale calculation
            var oldScale = _currentScale;
            var newScale = _startScale * e.Scale;
            newScale = Math.Max(MinScale, Math.Min(MaxScale, newScale));

            if (Math.Abs(newScale - oldScale) < 0.001) return;

            // Bei Anchor 0,0 (top-left): Zoom erfolgt von oben links
            // Translation wird NICHT angepasst während des Zooms
            _currentScale = newScale;

            // Update immediately
            _contentHost.Scale = _currentScale;

            // Update BindableProperty
            SetValue(ZoomProperty, _currentScale);
        }
        else if (e.Status == GestureStatus.Completed || e.Status == GestureStatus.Canceled)
        {
            _isPinching = false;

            // Clamp translation nur NACH dem Zoom
            ClampTranslation();
            _contentHost.TranslationX = _xOffset;
            _contentHost.TranslationY = _yOffset;
        }
    }

    private double _lastPanX = 0;
    private double _lastPanY = 0;

    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        // Ignore pan gestures while pinching
        if (_isPinching)
            return;

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

    // Item tap handler
    private void OnItemTapped(IPositionBase item)
    {
        if (ItemTappedCommand?.CanExecute(item) == true)
        {
            ItemTappedCommand.Execute(item);
        }
    }

    // Item-specific gesture handlers
    private double _itemStartX = 0;
    private double _itemStartY = 0;

    private void OnItemPanUpdated(object? sender, PanUpdatedEventArgs e, IPositionBase item, ContentView itemView)
    {
        if (!IsEditMode) return;

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _itemStartX = item.Xposition.ToDouble();
                _itemStartY = item.Yposition.ToDouble();
                break;

            case GestureStatus.Running:
                // Update position directly without scale adjustment
                // The gesture recognizer provides values in the view's coordinate system
                var newX = _itemStartX + e.TotalX;
                var newY = _itemStartY + e.TotalY;

                item.Xposition = newX.ToDecimal();
                item.Yposition = newY.ToDecimal();

                // Update layout position
                AbsoluteLayout.SetLayoutBounds(itemView, new Rect(newX, newY, item.Width.ToDouble(), item.Height.ToDouble()));
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                break;
        }
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

        // For top-left anchored content:
        // - Min translation is negative (content scrolls left/up)
        // - Max translation is 0 (content at top-left)

        double minTranslateX, minTranslateY;

        if (scaledWidth <= viewportWidth)
        {
            // Content fits in viewport horizontally - no scrolling needed
            minTranslateX = 0;
        }
        else
        {
            // Content is larger - allow scrolling to show all content
            minTranslateX = -(scaledWidth - viewportWidth);
        }

        if (scaledHeight <= viewportHeight)
        {
            // Content fits in viewport vertically - no scrolling needed
            minTranslateY = 0;
        }
        else
        {
            // Content is larger - allow scrolling to show all content
            minTranslateY = -(scaledHeight - viewportHeight);
        }

        // Clamp translation: min (to show right/bottom edge) <= offset <= 0 (top-left)
        _xOffset = Math.Max(minTranslateX, Math.Min(0, _xOffset));
        _yOffset = Math.Max(minTranslateY, Math.Min(0, _yOffset));
    }
}
