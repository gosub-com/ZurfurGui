using System.Diagnostics;
using ZurfurGui.Base;
using ZurfurGui.Platform;
using ZurfurGui.Property;
using ZurfurGui.Controls;

namespace ZurfurGui.Render;

/// <summary>
/// Keep track of the pointer hover target and send pointer events
/// </summary>
internal class PointerOver
{
    readonly AppWindow _appWindow;
    View? _hoverView;
    Point _pointerPosition;
    List<View> _pointerCaptureList = new();
    List<View> _hoverChain = new();

    public Point PointerPosition => _pointerPosition;

    public PointerOver(AppWindow appWindow)
    {
        _appWindow = appWindow;
    }

    /// <summary>
    /// Called when pointer input is received
    /// </summary>
    public void PointerInput(PointerEvent ev)
    {
        _pointerPosition = ev.Position;

        // Perform capture
        if (_pointerCaptureList.Count != 0)
        {
            // Send events to captured views
            if (ev.Type == "pointerup" || ev.Type == "pointerleave")
                ClearPointerCaptureList();
            else
                SendPointerEvent(ev, _pointerCaptureList);
        }

        // Find hit target chain
        var chain = new List<View>();
        View? hit;
        if (ev.Type == "pointerleave")
            hit = null;
        else
            hit = FindHitTarget(_appWindow.View, ev.Position);
        GetViewChain(hit, chain);

        // Update hover target        
        if (hit != _hoverView)
        {
            _hoverView = hit;

            // Add views to hover chain
            foreach (var view in chain)
            {
                if (!_hoverChain.Contains(view))
                {
                    _hoverChain.Add(view);
                    view.SetProperty(Zui.IsPointerOver, true);
                }
            }
            // Remove views from hover chain
            _hoverChain.RemoveAll(view =>
            {
                if (!chain.Contains(view))
                {
                    view.SetProperty(Zui.IsPointerOver, false);
                    return true;
                }
                return false;
            });
        }

        SendPointerEvent(ev, chain);
    }

    static View? FindHitTarget(View view, Point target)
    {
        // Quick exit when not visible or not in clip region
        var clip = new Rect(view.Origin, view.toDevice(view.Size));
        if (!clip.Contains(target))
            return null;

        if (!view.GetStyle(Zui.IsVisible).Or(true))
            return null;

        // Check children first
        var views = view.Children;
        for (var i = views.Count - 1; i >= 0; i--)
        {
            var hit = FindHitTarget(views[i], target);
            if (hit != null)
                return hit;
        }

        if (!view.GetProperty(Zui.DisableHitTest).Or(false))
        {
            // User content hit test
            if (view.Draw is Drawable renderable)
                if (renderable.IsHit(view, target))
                    return view;

            // Panel hit test
            if (DrawHelper.IsHitPanel(view, target))
                return view;
        }

        return null;
    }

    private static void SendPointerEvent(PointerEvent ev, List<View> views)
    {
        PropertyKey<EventHandler<PointerEvent>> property;
        switch (ev.Type)
        {
            case "pointermove": property = Zui.PreviewPointerMove; break;
            case "pointerdown": property = Zui.PreviewPointerDown; break;
            case "pointerup": property = Zui.PreviewPointerUp; break;
            default: return;
        }

        // Preview
        for (int i = views.Count - 1; i >= 0; i--)
        {
            var view = views[i];
            view.GetProperty(property)?.Invoke(null, ev);
        }

        switch (ev.Type)
        {
            case "pointermove": property = Zui.PointerMove; break;
            case "pointerdown": property = Zui.PointerDown; break;
            case "pointerup": property = Zui.PointerUp; break;
            default: return;
        }

        // Bubble
        foreach (var view in views)
        {
            view.GetProperty(property)?.Invoke(null, ev);
        }
    }

    /// <summary>
    /// Retrieve views from the given child up to the root
    /// </summary>
    static void GetViewChain(View? view, List<View> views)
    {
        while (view != null)
        {
            views.Add(view);
            view = view.Parent;
        }
    }
    internal bool GetIsPointerCaptured(View view)
    {
        return _pointerCaptureList.Contains(view);
    }

    internal void SetIsPointerCapture(View view, bool capture)
    {
        Debug.WriteLine($"Capture {capture}");
        var i = _pointerCaptureList.IndexOf(view);
        if (capture)
        {
            // TBD: Throw if not in pointer down
            if (i < 0)
                _pointerCaptureList.Add(view);
        }
        if (!capture)
        {
            if (i >= 0)
            {
                _pointerCaptureList.RemoveAt(i);
                view.GetProperty(Zui.PointerCaptureLost)?.Invoke(view, EventArgs.Empty);
            }
        }
    }

    internal void ClearPointerCaptureList()
    {
        Debug.WriteLine("CaptureLost");
        var c = _pointerCaptureList;
        _pointerCaptureList = new();
        foreach (var view in c)
            view.GetProperty(Zui.PointerCaptureLost)?.Invoke(view, EventArgs.Empty);
    }


}
