using Android.Views;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using Orderlyze.Foundation.Helper.Extensions;
using SharedControlsModule.PlatformControls;
using AView = Android.Views.View;
using Android.Graphics;

namespace Orderlyze.Platforms.Android.Handlers;

public class ZoomPanCanvasHandler : ContentViewHandler
{
    private ScaleGestureDetector? _scaleDetector;
    private GestureDetector? _gestureDetector;
    private Matrix _matrix = new Matrix();
    private Matrix _savedMatrix = new Matrix();
    private float[] _matrixValues = new float[9];
    private bool _isScaling = false;

    protected override ContentViewGroup CreatePlatformView()
    {
        var view = base.CreatePlatformView();

        // Create native Android gesture detectors
        _scaleDetector = new ScaleGestureDetector(Context, new ScaleListener(this));
        _gestureDetector = new GestureDetector(Context, new PanListener(this));

        return view;
    }

    protected override void ConnectHandler(ContentViewGroup platformView)
    {
        base.ConnectHandler(platformView);

        // Sync initial scale and translation from VirtualView
        if (VirtualView is ZoomPanCanvas canvas)
        {
            var scale = (float)canvas.Zoom;
            var contentHost = canvas.ContentHost;
            if (contentHost != null)
            {
                var translateX = (float)contentHost.TranslationX;
                var translateY = (float)contentHost.TranslationY;

                // Initialize matrix with current values
                _matrix.SetScale(scale, scale);
                _matrix.PostTranslate(translateX, translateY);
            }
            else
            {
                _matrix.SetScale(scale, scale);
            }
        }

        // Override touch handling
        platformView.Touch += OnTouch;

        // Mouse wheel support for Android Emulator / Desktop
        platformView.GenericMotion += OnGenericMotion;
    }

    protected override void DisconnectHandler(ContentViewGroup platformView)
    {
        platformView.Touch -= OnTouch;
        platformView.GenericMotion -= OnGenericMotion;
        base.DisconnectHandler(platformView);
    }

    private void OnTouch(object? sender, AView.TouchEventArgs e)
    {
        if (e.Event == null) return;

        // Let both detectors process the event
        var scaleHandled = _scaleDetector?.OnTouchEvent(e.Event) ?? false;
        var gestureHandled = _gestureDetector?.OnTouchEvent(e.Event) ?? false;

        e.Handled = scaleHandled || gestureHandled;
    }

    private void OnGenericMotion(object? sender, AView.GenericMotionEventArgs e)
    {
        if (e.Event == null) return;

        // Handle mouse wheel scrolling (for Android Emulator / Desktop testing)
        if (e.Event.Action == MotionEventActions.Scroll)
        {
            var scrollDelta = e.Event.GetAxisValue(Axis.Vscroll);

            if (Math.Abs(scrollDelta) > 0.01f)
            {
                // Get current scale
                _matrix.GetValues(_matrixValues);
                var currentScale = _matrixValues[Matrix.MscaleX];

                // Zoom in/out based on scroll direction
                var zoomFactor = scrollDelta > 0 ? 1.1f : 0.9f;
                var newScale = currentScale * zoomFactor;

                // Clamp scale
                newScale = Math.Max(ZoomPanCanvas.MinScale.ToFloat(), Math.Min(ZoomPanCanvas.MaxScale.ToFloat(), newScale));

                if (Math.Abs(newScale - currentScale) > 0.001f)
                {
                    // Scale from top-left (0,0)
                    var scaleFactor = newScale / currentScale;
                    _matrix.PostScale(scaleFactor, scaleFactor, 0, 0);

                    ApplyMatrixTransformation();
                }

                e.Handled = true;
            }
        }
    }

    private void ApplyMatrixTransformation(bool clamp = true)
    {
        if (VirtualView is not ZoomPanCanvas canvas) return;

        // Clamp translation if requested
        if (clamp)
        {
            ClampMatrix(canvas);
        }

        // Extract values from matrix
        _matrix.GetValues(_matrixValues);
        var scale = _matrixValues[Matrix.MscaleX];
        var translateX = _matrixValues[Matrix.MtransX];
        var translateY = _matrixValues[Matrix.MtransY];

        // Apply to ContentHost
        var contentHost = canvas.ContentHost;
        if (contentHost != null)
        {
            contentHost.Scale = scale;
            contentHost.TranslationX = translateX;
            contentHost.TranslationY = translateY;
        }

        // Sync internal offsets
        canvas.SyncInternalOffsets(translateX, translateY);

        // Update the BindableProperty
        canvas.SetValue(ZoomPanCanvas.ZoomProperty, (double)scale);
    }

    private void ClampMatrix(ZoomPanCanvas canvas)
    {
        // Get content and viewport dimensions from the virtual view
        var contentWidth = (float)canvas.ContentWidth;
        var contentHeight = (float)canvas.ContentHeight;
        var viewportWidth = (float)canvas.Width;
        var viewportHeight = (float)canvas.Height;

        if (contentWidth <= 0 || contentHeight <= 0 || viewportWidth <= 0 || viewportHeight <= 0)
            return;

        // Extract current values
        _matrix.GetValues(_matrixValues);
        var scale = _matrixValues[Matrix.MscaleX];
        var translateX = _matrixValues[Matrix.MtransX];
        var translateY = _matrixValues[Matrix.MtransY];

        // Calculate scaled content dimensions
        var scaledWidth = contentWidth * scale;
        var scaledHeight = contentHeight * scale;

        // Calculate boundaries
        float minTranslateX, minTranslateY;

        if (scaledWidth <= viewportWidth)
        {
            minTranslateX = 0;
        }
        else
        {
            minTranslateX = -(scaledWidth - viewportWidth);
        }

        if (scaledHeight <= viewportHeight)
        {
            minTranslateY = 0;
        }
        else
        {
            minTranslateY = -(scaledHeight - viewportHeight);
        }

        // Calculate clamped values
        var clampedX = Math.Max(minTranslateX, Math.Min(0, translateX));
        var clampedY = Math.Max(minTranslateY, Math.Min(0, translateY));

        // Only update if clamping is needed
        if (Math.Abs(clampedX - translateX) > 0.1f || Math.Abs(clampedY - translateY) > 0.1f)
        {
            // Calculate the delta to adjust
            var deltaX = clampedX - translateX;
            var deltaY = clampedY - translateY;

            // Apply the correction using PostTranslate to preserve the matrix state
            _matrix.PostTranslate(deltaX, deltaY);
        }
    }

    private class ScaleListener : ScaleGestureDetector.SimpleOnScaleGestureListener
    {
        private readonly ZoomPanCanvasHandler _handler;

        public ScaleListener(ZoomPanCanvasHandler handler)
        {
            _handler = handler;
        }

        public override bool OnScaleBegin(ScaleGestureDetector? detector)
        {
            // Save current matrix state
            _handler._savedMatrix.Set(_handler._matrix);
            _handler._isScaling = true;
            return true;
        }

        public override bool OnScale(ScaleGestureDetector? detector)
        {
            if (detector == null) return false;

            // Get current scale from matrix
            _handler._matrix.GetValues(_handler._matrixValues);
            var currentScale = _handler._matrixValues[Matrix.MscaleX];

            // Calculate scale factor
            var scaleFactor = detector.ScaleFactor;
            var newScale = currentScale * scaleFactor;

            // Clamp scale
            newScale = Math.Max(ZoomPanCanvas.MinScale.ToFloat(), Math.Min(ZoomPanCanvas.MaxScale.ToFloat(), newScale));

            if (Math.Abs(newScale - currentScale) < 0.001f) return true;

            // Scale around the focus point
            var actualScaleFactor = newScale / currentScale;
            _handler._matrix.PostScale(actualScaleFactor, actualScaleFactor, detector.FocusX, detector.FocusY);

            // Apply immediately without clamping (for smooth pinch)
            if (_handler.VirtualView is ZoomPanCanvas canvas)
            {
                _handler._matrix.GetValues(_handler._matrixValues);
                var scale = _handler._matrixValues[Matrix.MscaleX];
                var translateX = _handler._matrixValues[Matrix.MtransX];
                var translateY = _handler._matrixValues[Matrix.MtransY];

                var contentHost = canvas.ContentHost;
                if (contentHost != null)
                {
                    contentHost.Scale = scale;
                    contentHost.TranslationX = translateX;
                    contentHost.TranslationY = translateY;
                }
            }

            return true;
        }

        public override void OnScaleEnd(ScaleGestureDetector? detector)
        {
            // Reset flag and apply final transformation with clamped translation
            _handler._isScaling = false;
            _handler.ApplyMatrixTransformation();
        }
    }

    private class PanListener : GestureDetector.SimpleOnGestureListener
    {
        private readonly ZoomPanCanvasHandler _handler;
        private bool _isScrolling = false;

        public PanListener(ZoomPanCanvasHandler handler)
        {
            _handler = handler;
        }

        public override bool OnScroll(MotionEvent? e1, MotionEvent? e2, float distanceX, float distanceY)
        {
            // Don't pan while scaling
            if (_handler._isScaling)
                return false;

            _isScrolling = true;

            // Translate the matrix (note: distance is opposite direction)
            _handler._matrix.PostTranslate(-distanceX, -distanceY);

            // Check if we need to clamp (with some over-scroll tolerance)
            if (_handler.VirtualView is ZoomPanCanvas canvas)
            {
                var contentWidth = (float)canvas.ContentWidth;
                var contentHeight = (float)canvas.ContentHeight;
                var viewportWidth = (float)canvas.Width;
                var viewportHeight = (float)canvas.Height;

                if (contentWidth > 0 && contentHeight > 0 && viewportWidth > 0 && viewportHeight > 0)
                {
                    _handler._matrix.GetValues(_handler._matrixValues);
                    var scale = _handler._matrixValues[Matrix.MscaleX];
                    var translateX = _handler._matrixValues[Matrix.MtransX];
                    var translateY = _handler._matrixValues[Matrix.MtransY];

                    var scaledWidth = contentWidth * scale;
                    var scaledHeight = contentHeight * scale;

                    // Allow 50 pixels over-scroll before clamping
                    const float overScrollTolerance = 50f;

                    float maxX = overScrollTolerance;
                    float minX = scaledWidth <= viewportWidth ? 0 : -(scaledWidth - viewportWidth) - overScrollTolerance;
                    float maxY = overScrollTolerance;
                    float minY = scaledHeight <= viewportHeight ? 0 : -(scaledHeight - viewportHeight) - overScrollTolerance;

                    // Only clamp if significantly outside bounds
                    if (translateX > maxX || translateX < minX || translateY > maxY || translateY < minY)
                    {
                        var clampedX = Math.Max(minX, Math.Min(maxX, translateX));
                        var clampedY = Math.Max(minY, Math.Min(maxY, translateY));

                        var deltaX = clampedX - translateX;
                        var deltaY = clampedY - translateY;

                        _handler._matrix.PostTranslate(deltaX, deltaY);
                    }
                }
            }

            // Apply transformation without additional clamping
            _handler.ApplyMatrixTransformation(clamp: false);

            return true;
        }

        public override bool OnDown(MotionEvent? e)
        {
            // Stop any scrolling flag
            _isScrolling = false;

            // Ensure matrix is in sync at the start of pan - but only if significantly different
            if (_handler.VirtualView is ZoomPanCanvas canvas)
            {
                var contentHost = canvas.ContentHost;
                if (contentHost != null)
                {
                    // Get current matrix values
                    _handler._matrix.GetValues(_handler._matrixValues);
                    var currentScale = _handler._matrixValues[Matrix.MscaleX];
                    var currentTranslateX = _handler._matrixValues[Matrix.MtransX];
                    var currentTranslateY = _handler._matrixValues[Matrix.MtransY];

                    // Get ContentHost values
                    var hostScale = (float)contentHost.Scale;
                    var hostTranslateX = (float)contentHost.TranslationX;
                    var hostTranslateY = (float)contentHost.TranslationY;

                    // Only sync if there's a significant difference (> 1 pixel or 0.01 scale)
                    if (Math.Abs(currentScale - hostScale) > 0.01f ||
                        Math.Abs(currentTranslateX - hostTranslateX) > 1.0f ||
                        Math.Abs(currentTranslateY - hostTranslateY) > 1.0f)
                    {
                        // Update matrix with current values
                        _handler._matrix.SetValues(new float[]
                        {
                            hostScale, 0, hostTranslateX,
                            0, hostScale, hostTranslateY,
                            0, 0, 1
                        });
                    }
                }
            }

            return base.OnDown(e);
        }

        public override bool OnFling(MotionEvent? e1, MotionEvent? e2, float velocityX, float velocityY)
        {
            // Reset scrolling flag
            _isScrolling = false;
            // Don't consume the event - return false to allow default behavior
            return false;
        }
    }
}
