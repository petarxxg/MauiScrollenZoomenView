using CoreGraphics;
using Microsoft.Maui.Handlers;
using Orderlyze.Foundation.Helper.Extensions;
using SharedControlsModule.PlatformControls;
using System.Runtime.InteropServices;
using UIKit;
using PlatformView = Microsoft.Maui.Platform.ContentView;

namespace Orderlyze.Platforms.iOS.Handlers;

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

        // Sync initial scale from VirtualView
        if (VirtualView is ZoomPanCanvas canvas)
        {
            _scaleFactor = (nfloat)canvas.Scale;
        }

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
            // Sync scale from VirtualView at the start of gesture
            if (VirtualView is ZoomPanCanvas canvas)
            {
                _scaleFactor = (nfloat)canvas.Scale;
            }

            _lastScale = _scaleFactor;
        }
        else if (gesture.State == UIGestureRecognizerState.Changed)
        {
            // Calculate new scale
            var oldScale = _scaleFactor;
            var newScale = _lastScale * gesture.Scale;

            // Clamp scale
            newScale = (NFloat)(Math.Max(ZoomPanCanvas.MinScale, Math.Min(ZoomPanCanvas.MaxScale, newScale)));

            // Bei Anchor 0,0 (top-left): Zoom von oben links
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

        if (gesture.State == UIGestureRecognizerState.Began)
        {
            // Sync scale from VirtualView at the start of pan gesture
            if (VirtualView is ZoomPanCanvas canvas)
            {
                _scaleFactor = (nfloat)canvas.Scale;
            }
        }
        else if (gesture.State == UIGestureRecognizerState.Changed)
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

        // Clamp translation to prevent scrolling outside content bounds
        ClampTranslation(canvas);

        // Access the ContentHost directly
        var contentHost = canvas.ContentHost;
        if (contentHost != null)
        {
            contentHost.Scale = (double)_scaleFactor;
            contentHost.TranslationX = (double)_translateX;
            contentHost.TranslationY = (double)_translateY;
        }

        // Update the BindableProperty
        canvas.SetValue(ZoomPanCanvas.ScaleProperty, (double)_scaleFactor);
    }

    private void ClampTranslation(ZoomPanCanvas canvas)
    {
        // Get content and viewport dimensions from the virtual view
        var contentWidth = (nfloat)canvas.ContentWidth;
        var contentHeight = (nfloat)canvas.ContentHeight;
        var viewportWidth = (nfloat)canvas.Width;
        var viewportHeight = (nfloat)canvas.Height;

        if (contentWidth <= 0 || contentHeight <= 0 || viewportWidth <= 0 || viewportHeight <= 0)
            return;

        // Calculate scaled content dimensions
        var scaledWidth = contentWidth * _scaleFactor;
        var scaledHeight = contentHeight * _scaleFactor;

        // For top-left anchored content (Anchor 0,0):
        // - Min translation is negative (content scrolls left/up)
        // - Max translation is 0 (content at top-left)
        nfloat minTranslateX, minTranslateY;

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
        _translateX = (nfloat)Math.Max(minTranslateX, Math.Min(0, _translateX));
        _translateY = (nfloat)Math.Max(minTranslateY, Math.Min(0, _translateY));
    }
}
