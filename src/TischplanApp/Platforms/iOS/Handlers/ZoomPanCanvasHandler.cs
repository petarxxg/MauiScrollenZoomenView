using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using TischplanApp.Controls;
using UIKit;
using CoreGraphics;
using PlatformView = Microsoft.Maui.Platform.ContentView;

namespace TischplanApp.Platforms.iOS.Handlers;

public class ZoomPanCanvasHandler : ContentViewHandler
{
    private UIPinchGestureRecognizer? _pinchGestureRecognizer;
    private UIPanGestureRecognizer? _panGestureRecognizer;
    private nfloat _scaleFactor = 1.0f;
    private nfloat _translateX = 0f;
    private nfloat _translateY = 0f;
    private nfloat _lastScale = 1.0f;

    protected override PlatformView CreatePlatformView()
    {
        var view = base.CreatePlatformView();

        // Create native iOS gesture recognizers
        _pinchGestureRecognizer = new UIPinchGestureRecognizer(OnPinch);
        _panGestureRecognizer = new UIPanGestureRecognizer(OnPan);

        // Allow simultaneous gestures
        _pinchGestureRecognizer.ShouldRecognizeSimultaneously = (g1, g2) => true;
        _panGestureRecognizer.ShouldRecognizeSimultaneously = (g1, g2) => true;

        view.AddGestureRecognizer(_pinchGestureRecognizer);
        view.AddGestureRecognizer(_panGestureRecognizer);

        return view;
    }

    protected override void DisconnectHandler(PlatformView platformView)
    {
        if (_pinchGestureRecognizer != null)
        {
            platformView.RemoveGestureRecognizer(_pinchGestureRecognizer);
            _pinchGestureRecognizer.Dispose();
            _pinchGestureRecognizer = null;
        }

        if (_panGestureRecognizer != null)
        {
            platformView.RemoveGestureRecognizer(_panGestureRecognizer);
            _panGestureRecognizer.Dispose();
            _panGestureRecognizer = null;
        }

        base.DisconnectHandler(platformView);
    }

    private void OnPinch(UIPinchGestureRecognizer gesture)
    {
        if (gesture.State == UIGestureRecognizerState.Began)
        {
            _lastScale = _scaleFactor;
        }
        else if (gesture.State == UIGestureRecognizerState.Changed)
        {
            // Calculate new scale
            var oldScale = _scaleFactor;
            var newScale = _lastScale * gesture.Scale;

            // Clamp scale
            newScale = (nfloat)Math.Max(0.1, Math.Min(3.0, newScale));

            // Bei zentriertem Canvas (Anchor 0.5): Translation proportional skalieren!
            // Was du gerade siehst, wird einfach größer/kleiner OHNE Verschiebung
            var scaleRatio = newScale / oldScale;
            _translateX *= scaleRatio;
            _translateY *= scaleRatio;
            _scaleFactor = newScale;

            // Apply transformation on main thread
            Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
            {
                ApplyTransformation();
            });
        }
    }

    private void OnPan(UIPanGestureRecognizer gesture)
    {
        if (PlatformView == null) return;

        if (gesture.State == UIGestureRecognizerState.Changed)
        {
            // Get translation delta
            var translation = gesture.TranslationInView(PlatformView);

            // Update translation
            _translateX += translation.X;
            _translateY += translation.Y;

            // Reset gesture translation to get delta on next call
            gesture.SetTranslation(CGPoint.Empty, PlatformView);

            // Apply transformation on main thread
            Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
            {
                ApplyTransformation();
            });
        }
    }

    private void ApplyTransformation()
    {
        if (VirtualView is not ZoomPanCanvas canvas) return;

        // Access the ContentHost directly
        var contentHost = canvas.ContentHost;
        if (contentHost != null)
        {
            contentHost.Scale = (double)_scaleFactor;
            contentHost.TranslationX = (double)_translateX;
            contentHost.TranslationY = (double)_translateY;
        }
    }
}
