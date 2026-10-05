using System.Diagnostics;
using ZurfurGui.Base;
using ZurfurGui.Controls;
using ZurfurGui.Property;
using ZurfurGui.Render;
using ZurfurGui.Platform;
using ZurfurGui.Windows;


namespace ZurfurGui.Input;

/// <summary>
/// Keep track of the pointer hover target and send pointer events
/// </summary>
internal class PointerOver
{
    readonly AppWindow _appWindow;
    Point _pointerDevicePosition;
    readonly Dictionary<PointerContactKey, PointerContactState> _contactStates = new();
    PointerContactState? _activeContactState;

    public Point PointerDevicePosition => _pointerDevicePosition;

    public PointerOver(AppWindow appWindow)
    {
        _appWindow = appWindow;
    }

    /// <summary>
    /// Called when pointer input is received
    /// </summary>
    public void PointerInput(OsPointerEvent rawEvent)
    {
        var ev = rawEvent.ToPointerEvent();
        if (ev.Kind == PointerEventKind.Unknown)
            return;

        _pointerDevicePosition = ev.Contact.DevicePosition;
        var state = GetContactState(ev.Contact);
        _activeContactState = state;
        var capturedViews = state.CapturedViews.ToList();
        var wasCaptured = capturedViews.Count != 0;

        // Perform capture
        if (wasCaptured)
        {
            // Send events to captured views
            SendPointerEvent(ev, capturedViews);

            // Clear events on up or cancel
            if (ev.Kind is PointerEventKind.Up or PointerEventKind.Cancel)
                ClearPointerCaptureList(state);
        }

        // Find hit target chain
        View? hit;
        if (ev.Kind == PointerEventKind.Leave)
            hit = null;
        else
            hit = FindHitTarget(_appWindow.View, new HitTestContext(ev.Contact));

        var chain = GetViewChain(hit);

        // Update press target
        if (ev.Kind == PointerEventKind.Down)
            state.SetPressChain(chain);

        // Update hover target
        if (hit != state.HoverView || ev.Kind == PointerEventKind.Down || ev.Kind == PointerEventKind.Up)
        {
            var previousHoverChain = state.HoverChain.ToList();
            var previousPressChain = state.CurrentPressChain.ToList();
            state.UpdateHover(hit, chain);
            UpdateViewChain(previousHoverChain, state.HoverChain, Panel.IsPointerOver);
            UpdateViewChain(previousPressChain, state.CurrentPressChain, Panel.IsPressed);
        }

        // A captured control remains pressed until capture is released, even after the pointer leaves its
        // original hover chain. This is required for drag operations and pressed visuals outside the control.
        foreach (var capturedView in state.CapturedViews)
            capturedView.SetProperty(Panel.IsPressed, true);

        // Send low level mouse event (move, up, down)
        // A captured event was already sent to the capture route above. Do not also send it to the hit route.
        if (!wasCaptured)
            SendPointerEvent(ev, chain);

        // Send click event
        if (ev.Kind == PointerEventKind.Up)
        {
            var hadPress = state.PressChain.Count != 0;
            if (hadPress && state.CurrentPressChain.Count != 0)
                SendPointerEvent(ev with { Kind = PointerEventKind.Click }, state.CurrentPressChain);
            var previousPressChain = state.CurrentPressChain.ToList();
            state.SetPressChain(Array.Empty<View>());
            state.UpdateCurrentPressChain();
            UpdateViewChain(previousPressChain, state.CurrentPressChain, Panel.IsPressed);
        }
        else if (ev.Kind == PointerEventKind.Cancel || (ev.Kind == PointerEventKind.Leave && !wasCaptured))
        {
            RemoveContactState(ev.Contact, state);
        }

        _activeContactState = null;
    }

    private PointerContactState GetContactState(PointerContact contact)
    {
        var key = new PointerContactKey(contact.Device, contact.Id);
        if (!_contactStates.TryGetValue(key, out var state))
        {
            state = new PointerContactState();
            _contactStates.Add(key, state);
        }

        state.Contact = contact;
        return state;
    }

    private void UpdateViewChain(List<View> updateChain, List<View> newChain, PropertyKey<bool> property)
    {
        var affectedViews = updateChain
            .Concat(newChain)
            .Distinct()
            .ToList();

        updateChain.Clear();
        updateChain.AddRange(newChain);

        foreach (var view in affectedViews)
            RecomputeViewProperty(view, property);
    }

    private void RecomputeViewProperty(View view, PropertyKey<bool> property)
    {
        var value = property.Id == Panel.IsPointerOver.Id
            ? _contactStates.Values.Any(state => state.HoverChain.Contains(view))
            : _contactStates.Values.Any(state =>
                state.CurrentPressChain.Contains(view) || state.CapturedViews.Contains(view));

        view.SetProperty(property, value);
    }

    private void RemoveContactState(PointerContact contact, PointerContactState state)
    {
        var capturedViews = state.ReleaseAllCapture();
        var affectedViews = state.Reset();
        _contactStates.Remove(new PointerContactKey(contact.Device, contact.Id));

        foreach (var view in capturedViews)
            view.GetProperty(Panel.PointerCaptureLost)?.Invoke(view, EventArgs.Empty);

        foreach (var view in affectedViews)
            RecomputeViewProperty(view, Panel.IsPointerOver);
        foreach (var view in affectedViews)
            RecomputeViewProperty(view, Panel.IsPressed);
    }

    static View? FindHitTarget(View view, HitTestContext context)
    {
        // Quick exit when not visible or not in clip region
        var clip = view.OriginRect;
        if (!clip.Contains(context.Contact.DevicePosition))
            return null;

        if (!view.GetStyle(Panel.IsVisible))
            return null;

        // Check children first
        var views = view.Children;
        for (var i = views.Count - 1; i >= 0; i--)
        {
            var hit = FindHitTarget(views[i], context);
            if (hit != null)
                return hit;
        }

        var hitTest = view.GetProperty(Panel.HitTest);
        if (hitTest == HitTestMode.Disabled)
            return null;

        if (hitTest == HitTestMode.Always)
            return view;

        // User content hit test
        if (view.Render is Renderable renderable)
            if (renderable.IsHit(view, context))
                return view;

        // Panel hit test
        if (RenderHelper.IsHitPanel(view, context))
            return view;

        return null;
    }

    private static void SendPointerEvent(PointerEvent ev, List<View> views)
    {
        // Handlers may change capture while the event is routed, so dispatch against a stable route snapshot.
        views = views.ToList();

        PropertyKey<EventHandler<PointerEvent>> property;
        switch (ev.Kind)
        {
            case PointerEventKind.Move: property = Panel.PreviewPointerMove; break;
            case PointerEventKind.Down: property = Panel.PreviewPointerDown; break;
            case PointerEventKind.Up: property = Panel.PreviewPointerUp; break;
            case PointerEventKind.Click: property = Panel.PreviewPointerClick; break;
            default: return;
        }

        // Preview
        for (int i = views.Count - 1; i >= 0; i--)
        {
            var view = views[i];
            view.GetProperty(property)?.Invoke(null, ev);
        }

        switch (ev.Kind)
        {
            case PointerEventKind.Move: property = Panel.PointerMove; break;
            case PointerEventKind.Down: property = Panel.PointerDown; break;
            case PointerEventKind.Up: property = Panel.PointerUp; break;
            case PointerEventKind.Click: property = Panel.PointerClick; break;
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
        return _contactStates.Values.Any(state => state.CapturedViews.Contains(view));
    }

    internal void SetIsPointerCapture(View view, bool capture)
    {
        Debug.WriteLine($"Capture {capture}");
        if (_activeContactState is not PointerContactState state)
            return;

        if (capture)
        {
            // TBD: Throw if not in pointer down
            if (state.Capture(view))
                view.SetProperty(Panel.IsPressed, true);
        }
        if (!capture)
        {
            if (state.Release(view))
            {
                view.GetProperty(Panel.PointerCaptureLost)?.Invoke(view, EventArgs.Empty);
                RecomputeViewProperty(view, Panel.IsPressed);
            }
        }
    }

    private void ClearPointerCaptureList(PointerContactState state)
    {
        Debug.WriteLine("CaptureLost");
        var c = state.ReleaseAllCapture();
        foreach (var view in c)
        {
            view.GetProperty(Panel.PointerCaptureLost)?.Invoke(view, EventArgs.Empty);
            RecomputeViewProperty(view, Panel.IsPressed);
        }
    }


}
