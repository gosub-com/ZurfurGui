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
    List<View> _pressChain = new();
    List<View> _currentPressChain = new();  // Intersect of _hoverchan and _presschain


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
        View? hit;
        if (ev.Type == "pointerleave")
            hit = null;
        else
            hit = FindHitTarget(_appWindow.View, ev.Position);

        var chain = GetViewChain(hit);

        // Update press target
        if (ev.Type == "pointerdown")
            _pressChain = chain;

        // Update hover target        
        if (hit != _hoverView || ev.Type == "pointerdown" || ev.Type == "pointerup")
        {
            _hoverView = hit;
            UpdateViewChain(_hoverChain, chain, Zui.IsPointerOver);
            UpdateViewChain(_currentPressChain, IntersectViewChain(_hoverChain, _pressChain), Zui.IsPressed);
        }

        // Send low level mouse event (move, up, down)
        SendPointerEvent(ev, chain);

        // Send click event
        if (ev.Type == "pointerup")
        {
           SendPointerEvent(ev with { Type = "pointerclick" }, _currentPressChain);
            _pressChain = new();
           UpdateViewChain(_currentPressChain, IntersectViewChain(_hoverChain, _pressChain), Zui.IsPressed);
        }
    }

    private static void UpdateViewChain(List<View> updateChain, List<View> newChain, PropertyKey<EnumProp<bool>> property)
    {
        // Add views to hover chain
        foreach (var view in newChain)
        {
            if (!updateChain.Contains(view))
            {
                updateChain.Add(view);
                view.SetProperty(property, true);
            }
        }
        // Remove views from hover chain
        updateChain.RemoveAll(view =>
        {
            if (!newChain.Contains(view))
            {
                view.SetProperty(property, false);
                return true;
            }
            return false;
        });
    }

    private static List<View> IntersectViewChain(List<View> chain1, List<View> chain2)
    {
        var result = new List<View>();
        foreach (var view in chain1)
        {
            if (chain2.Contains(view))
                result.Add(view);
        }
        return result;
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
            case "pointerclick": property = Zui.PreviewPointerClick; break;
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
            case "pointerclick": property = Zui.PointerClick; break;
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
    static List<View> GetViewChain(View? view)
    {
        var views = new List<View>();
        while (view != null)
        {
            views.Add(view);
            view = view.Parent;
        }
        return views;
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
