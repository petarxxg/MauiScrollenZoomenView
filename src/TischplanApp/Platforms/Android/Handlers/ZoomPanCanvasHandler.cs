using Android.Views;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using TischplanApp.Controls;
using AView = Android.Views.View;

namespace TischplanApp.Platforms.Android.Handlers;

public class ZoomPanCanvasHandler : ContentViewHandler
{
    private ScaleGestureDetector? _scaleDetector;
    private GestureDetector? _gestureDetector;
    private float _scaleFactor = 1.0f;
    private float _translateX = 0f;
    private float _translateY = 0f;
    private float _lastFocusX = 0f;
    private float _lastFocusY = 0f;

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
                newScale = Math.Max(0.1f, Math.Min(3.0f, newScale));

                if (Math.Abs(newScale - oldScale) > 0.001f)
                {
                    // Proportional translation scaling
                    var scaleRatio = newScale / oldScale;
                    _translateX *= scaleRatio;
                    _translateY *= scaleRatio;
                    _scaleFactor = newScale;

                    ApplyTransformation();
                }

                e.Handled = true;
            }
        }
    }

    private void ApplyTransformation()
    {
        if (VirtualView is not ZoomPanCanvas canvas) return;

        // Access the ContentHost directly
        var contentHost = canvas.ContentHost;
        if (contentHost != null)
        {
            contentHost.Scale = _scaleFactor;
            contentHost.TranslationX = _translateX;
            contentHost.TranslationY = _translateY;
        }
    }

    private class ScaleListener : ScaleGestureDetector.SimpleOnScaleGestureListener
    {
        private readonly ZoomPanCanvasHandler _handler;
        private float _startScale;

        public ScaleListener(ZoomPanCanvasHandler handler)
        {
            _handler = handler;
        }

        public override bool OnScaleBegin(ScaleGestureDetector? detector)
        {
            _startScale = _handler._scaleFactor;
            _handler._lastFocusX = detector?.FocusX ?? 0;
            _handler._lastFocusY = detector?.FocusY ?? 0;
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
            newScale = Math.Max(0.1f, Math.Min(3.0f, newScale));

            // Bei zentriertem Canvas (Anchor 0.5): Translation proportional skalieren!
            // Was du gerade siehst, wird einfach größer/kleiner OHNE Verschiebung
            var scaleRatio = newScale / oldScale;
            _handler._translateX *= scaleRatio;
            _handler._translateY *= scaleRatio;
            _handler._scaleFactor = newScale;

            // Apply transformation on UI thread
            Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
            {
                _handler.ApplyTransformation();
            });

            return true;
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
            // Update translation (note: distance is opposite direction)
            _handler._translateX -= distanceX;
            _handler._translateY -= distanceY;

            // Apply transformation on UI thread
            Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
            {
                _handler.ApplyTransformation();
            });

            return true;
        }
    }
}
