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

    private void ApplyMatrixTransformation()
    {
        if (VirtualView is not ZoomPanCanvas canvas) return;

        // Extract values from matrix
        _matrix.GetValues(_matrixValues);
        var scale = _matrixValues[Matrix.MscaleX];
        var translateX = _matrixValues[Matrix.MtransX];
        var translateY = _matrixValues[Matrix.MtransY];

        // Clamp translation
        ClampMatrix(canvas);

        // Re-extract after clamping
        _matrix.GetValues(_matrixValues);
        scale = _matrixValues[Matrix.MscaleX];
        translateX = _matrixValues[Matrix.MtransX];
        translateY = _matrixValues[Matrix.MtransY];

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

        // Clamp translation
        translateX = Math.Max(minTranslateX, Math.Min(0, translateX));
        translateY = Math.Max(minTranslateY, Math.Min(0, translateY));

        // Update matrix with clamped values
        _matrix.SetValues(new float[]
        {
            scale, 0, translateX,
            0, scale, translateY,
            0, 0, 1
        });
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

        public PanListener(ZoomPanCanvasHandler handler)
        {
            _handler = handler;
        }

        public override bool OnScroll(MotionEvent? e1, MotionEvent? e2, float distanceX, float distanceY)
        {
            // Don't pan while scaling
            if (_handler._isScaling)
                return false;

            // Translate the matrix (note: distance is opposite direction)
            _handler._matrix.PostTranslate(-distanceX, -distanceY);

            // Apply transformation with clamping
            _handler.ApplyMatrixTransformation();

            return true;
        }

        public override bool OnDown(MotionEvent? e)
        {
            // Ensure matrix is in sync at the start of pan
            if (_handler.VirtualView is ZoomPanCanvas canvas)
            {
                var contentHost = canvas.ContentHost;
                if (contentHost != null)
                {
                    var scale = (float)contentHost.Scale;
                    var translateX = (float)contentHost.TranslationX;
                    var translateY = (float)contentHost.TranslationY;

                    // Update matrix with current values
                    _handler._matrix.SetValues(new float[]
                    {
                        scale, 0, translateX,
                        0, scale, translateY,
                        0, 0, 1
                    });
                }
            }

            return base.OnDown(e);
        }
    }
}
