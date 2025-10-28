using Android.Views;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using Orderlyze.Foundation.Helper.Extensions;
using SharedControlsModule.PlatformControls;
using AView = Android.Views.View;

namespace Orderlyze.Platforms.Android.Handlers;

public class ZoomPanCanvasHandler : ContentViewHandler
{
    private ScaleGestureDetector? _scaleDetector;
    private GestureDetector? _gestureDetector;
    private float _scaleFactor = 1.0f;
    private float _translateX = 0f;
    private float _translateY = 0f;
    private float _lastFocusX = 0f;
    private float _lastFocusY = 0f;
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

        // Sync initial scale from VirtualView
        if (VirtualView is ZoomPanCanvas canvas)
        {
            _scaleFactor = (float)canvas.Zoom;
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
                // Zoom in/out based on scroll direction
                var zoomFactor = scrollDelta > 0 ? 1.1f : 0.9f;
                var oldScale = _scaleFactor;
                var newScale = oldScale * zoomFactor;

                // Clamp scale
                newScale = Math.Max(ZoomPanCanvas.MinScale.ToFloat(), Math.Min(ZoomPanCanvas.MaxScale.ToFloat(), newScale));

                if (Math.Abs(newScale - oldScale) > 0.001f)
                {
                    // Bei Anchor 0,0 (top-left): Zoom von oben links
                    _scaleFactor = newScale;

                    ApplyTransformation();
                }

                e.Handled = true;
            }
        }
    }

    private void ApplyTransformation(bool updateTranslation = true)
    {
        if (VirtualView is not ZoomPanCanvas canvas) return;

        // Access the ContentHost directly
        var contentHost = canvas.ContentHost;
        if (contentHost != null)
        {
            contentHost.Scale = _scaleFactor;

            // Only update translation if requested (not during active zoom)
            if (updateTranslation)
            {
                ClampTranslation(canvas);
                contentHost.TranslationX = _translateX;
                contentHost.TranslationY = _translateY;
            }
        }

        // Sync internal offsets BEFORE calling SetValue
        // This prevents SetScale() from overwriting our translation values
        canvas.SyncInternalOffsets(_translateX, _translateY);

        // Update the BindableProperty - this will trigger SetScale() but with synced offsets
        canvas.SetValue(ZoomPanCanvas.ZoomProperty, (double)_scaleFactor);
    }

    private void ClampTranslation(ZoomPanCanvas canvas)
    {
        // Get content and viewport dimensions from the virtual view
        var contentWidth = (float)canvas.ContentWidth;
        var contentHeight = (float)canvas.ContentHeight;
        var viewportWidth = (float)canvas.Width;
        var viewportHeight = (float)canvas.Height;

        if (contentWidth <= 0 || contentHeight <= 0 || viewportWidth <= 0 || viewportHeight <= 0)
            return;

        // Calculate scaled content dimensions
        var scaledWidth = contentWidth * _scaleFactor;
        var scaledHeight = contentHeight * _scaleFactor;

        // For top-left anchored content (Anchor 0,0):
        float minTranslateX, maxTranslateX, minTranslateY, maxTranslateY;

        if (scaledWidth <= viewportWidth)
        {
            // Content fits in viewport horizontally - allow positioning within viewport
            minTranslateX = 0;  // Content can be at left edge
            maxTranslateX = viewportWidth - scaledWidth;  // Content can be at right edge
        }
        else
        {
            // Content is larger - allow scrolling to show all content
            minTranslateX = -(scaledWidth - viewportWidth);  // Show right edge
            maxTranslateX = 0;  // Show left edge
        }

        if (scaledHeight <= viewportHeight)
        {
            // Content fits in viewport vertically - allow positioning within viewport
            minTranslateY = 0;  // Content can be at top edge
            maxTranslateY = viewportHeight - scaledHeight;  // Content can be at bottom edge
        }
        else
        {
            // Content is larger - allow scrolling to show all content
            minTranslateY = -(scaledHeight - viewportHeight);  // Show bottom edge
            maxTranslateY = 0;  // Show top edge
        }

        // Clamp translation to keep content within viewport
        _translateX = Math.Max(minTranslateX, Math.Min(maxTranslateX, _translateX));
        _translateY = Math.Max(minTranslateY, Math.Min(maxTranslateY, _translateY));
    }

    private class ScaleListener : ScaleGestureDetector.SimpleOnScaleGestureListener
    {
        private readonly ZoomPanCanvasHandler _handler;
        private float _startScale;
        private float _startTranslateX;
        private float _startTranslateY;

        public ScaleListener(ZoomPanCanvasHandler handler)
        {
            _handler = handler;
        }

        public override bool OnScaleBegin(ScaleGestureDetector? detector)
        {
            // Sync scale and translation from VirtualView at the start of gesture
            if (_handler.VirtualView is ZoomPanCanvas canvas)
            {
                _handler._scaleFactor = (float)canvas.Zoom;

                // Sync translation from ContentHost to avoid jumps
                var contentHost = canvas.ContentHost;
                if (contentHost != null)
                {
                    _handler._translateX = (float)contentHost.TranslationX;
                    _handler._translateY = (float)contentHost.TranslationY;
                }
            }

            _startScale = _handler._scaleFactor;
            _startTranslateX = _handler._translateX;
            _startTranslateY = _handler._translateY;
            _handler._lastFocusX = detector?.FocusX ?? 0;
            _handler._lastFocusY = detector?.FocusY ?? 0;
            _handler._isScaling = true;
            return true;
        }

        public override bool OnScale(ScaleGestureDetector? detector)
        {
            if (detector == null) return false;

            // Calculate new scale
            var scaleDelta = detector.ScaleFactor;
            var oldScale = _handler._scaleFactor;
            var newScale = oldScale * scaleDelta;

            // Clamp scale
            newScale = Math.Max(ZoomPanCanvas.MinScale.ToFloat(), Math.Min(ZoomPanCanvas.MaxScale.ToFloat(), newScale));

            if (Math.Abs(newScale - oldScale) < 0.001f) return true;

            // Convert focus point to view-relative coordinates
            var focusXScreen = detector.FocusX;
            var focusYScreen = detector.FocusY;

            // Get view position on screen
            var locationOnScreen = new int[2];
            _handler.PlatformView?.GetLocationOnScreen(locationOnScreen);
            var viewX = locationOnScreen[0];
            var viewY = locationOnScreen[1];

            // Calculate focus point relative to view
            var focusX = focusXScreen - viewX;
            var focusY = focusYScreen - viewY;

            var scaleRatio = newScale / _startScale;

            // Adjust translation to keep focus point fixed
            // Formula: newTranslation = focusPoint * (1 - scaleRatio) + startTranslation * scaleRatio
            _handler._translateX = focusX * (1 - scaleRatio) + _startTranslateX * scaleRatio;
            _handler._translateY = focusY * (1 - scaleRatio) + _startTranslateY * scaleRatio;

            _handler._scaleFactor = newScale;

            // Apply transformation directly to ContentHost during zoom
            if (_handler.VirtualView is ZoomPanCanvas canvas)
            {
                var contentHost = canvas.ContentHost;
                if (contentHost != null)
                {
                    contentHost.Scale = _handler._scaleFactor;
                    contentHost.TranslationX = _handler._translateX;
                    contentHost.TranslationY = _handler._translateY;
                }

                // DO NOT call SetValue during pinching! It would trigger SetScale() and reset translation
                // canvas.SetValue(ZoomPanCanvas.ZoomProperty, (double)_handler._scaleFactor);
            }

            return true;
        }

        public override void OnScaleEnd(ScaleGestureDetector? detector)
        {
            // Reset flag and apply final transformation with clamped translation
            _handler._isScaling = false;
            _handler.ApplyTransformation(updateTranslation: true);
        }
    }

    private class PanListener : GestureDetector.SimpleOnGestureListener
    {
        private readonly ZoomPanCanvasHandler _handler;
        private bool _hasCheckedScale = false;

        public PanListener(ZoomPanCanvasHandler handler)
        {
            _handler = handler;
        }

        public override bool OnScroll(MotionEvent? e1, MotionEvent? e2, float distanceX, float distanceY)
        {
            // Sync scale from VirtualView if not checked yet in this pan session
            if (!_hasCheckedScale && _handler.VirtualView is ZoomPanCanvas canvas)
            {
                var currentScale = (float)canvas.Zoom;
                if (Math.Abs(_handler._scaleFactor - currentScale) > 0.001f)
                {
                    _handler._scaleFactor = currentScale;
                }
                _hasCheckedScale = true;
            }

            // Update translation (note: distance is opposite direction)
            _handler._translateX -= distanceX;
            _handler._translateY -= distanceY;

            // Apply transformation directly without MainThread dispatch
            _handler.ApplyTransformation();

            return true;
        }

        public override bool OnDown(MotionEvent? e)
        {
            // Reset flag when new touch starts
            _hasCheckedScale = false;

            // Sync translation from ContentHost at the start of pan
            if (_handler.VirtualView is ZoomPanCanvas canvas)
            {
                var contentHost = canvas.ContentHost;
                if (contentHost != null)
                {
                    _handler._translateX = (float)contentHost.TranslationX;
                    _handler._translateY = (float)contentHost.TranslationY;
                }
            }

            return base.OnDown(e);
        }
    }
}
