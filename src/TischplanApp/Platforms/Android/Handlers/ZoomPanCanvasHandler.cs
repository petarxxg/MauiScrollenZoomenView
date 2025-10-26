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
    }

    protected override void DisconnectHandler(ContentViewGroup platformView)
    {
        platformView.Touch -= OnTouch;
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
            if (_handler.PlatformView == null) return false;

            // Calculate new scale
            var scaleDelta = detector.ScaleFactor;
            var newScale = _handler._scaleFactor * scaleDelta;

            // Clamp scale
            newScale = Math.Max(0.1f, Math.Min(3.0f, newScale));

            // Zoom auf die MITTE des Bildschirms (nicht auf Finger-Position!)
            var viewCenterX = _handler.PlatformView.Width / 2.0f;
            var viewCenterY = _handler.PlatformView.Height / 2.0f;

            // Calculate position in content coordinates before scaling (an der View-Mitte)
            var contentX = (viewCenterX - _handler._translateX) / _handler._scaleFactor;
            var contentY = (viewCenterY - _handler._translateY) / _handler._scaleFactor;

            // Update scale
            _handler._scaleFactor = newScale;

            // Adjust translation so view center stays at same content position
            _handler._translateX = viewCenterX - (contentX * _handler._scaleFactor);
            _handler._translateY = viewCenterY - (contentY * _handler._scaleFactor);

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
