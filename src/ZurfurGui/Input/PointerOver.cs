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
    /// Updates this contact's hover and press chains, dispatches changed pointer events, and routes the input event.
    /// </summary>
    public void PointerInput(OsPointerEvent rawEvent)
    {
        var ev = rawEvent.ToPointerEvent();
        if (ev.Kind == PointerEventKind.Unknown)
            return;

        _pointerDevicePosition = ev.Contact.DevicePosition;
        var state = GetContactState(ev.Contact);
        _activeContactState = state;
        var capturedView = state.CapturedView;
        var wasCaptured = capturedView is not null;

        // Route move/down/up events to captured views.
        if (capturedView is View captureTarget)
        {
            if (ev.Kind == PointerEventKind.Cancel)
            {
                // Platform cancel uses the same capture/press cancellation path as a parent cancel.
                CancelPointerCapture(state);
            }
            else
            {
                // Phases 2-3: Route captured down/move/up through preview and bubble.
                SendPointerEvent(ev, [captureTarget]);
            }

            // Phase 4: Pointer-up releases capture after captured routing.
            if (ev.Kind == PointerEventKind.Up)
                ClearPointerCapture(state);
        }

        // Phase 1: Find the hit target and build its view chain. Leave and cancel have no hit target.
        View? hit;
        if (ev.Kind is PointerEventKind.Leave or PointerEventKind.Cancel)
            hit = null;
        else
            hit = FindHitTarget(_appWindow.View, new HitTestContext(ev.Contact));

        var chain = GetViewChain(hit);

        // Phase 4: Update press and hover state from the hit chain; hover changes notify entering and exiting views.
        if (ev.Kind == PointerEventKind.Down)
            state.SetPressChain(chain);

        if (hit != state.HoverView || ev.Kind == PointerEventKind.Down || ev.Kind == PointerEventKind.Up)
        {
            var previousHoverChain = state.HoverChain.ToList();
            var previousPressChain = state.CurrentPressChain.ToList();
            state.UpdateHover(hit, chain);
            UpdateViewChain(previousHoverChain.ToList(), state.HoverChain, Panel.IsPointerOver);
            UpdateViewChain(previousPressChain, state.CurrentPressChain, Panel.IsPressed);
            NotifyHoverChainChanges(previousHoverChain, state.HoverChain, ev);
        }

        // A captured control remains pressed until capture is released, even after the pointer leaves its
        // original hover chain. This is required for drag operations and pressed visuals outside the control.
        state.CapturedView?.SetProperty(Panel.IsPressed, true);

        // Phases 2-3: Uncaptured down/move/up route through preview then bubble on the hit chain. Captured input
        // was already routed above, so it must not also be sent through the hit chain.
        if (!wasCaptured)
            SendPointerEvent(ev, chain);

        // Phase 4: Finish state cleanup after routing. Cancel and uncaptured leave also remove the contact state.
        if (ev.Kind == PointerEventKind.Up)
        {
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

    /// <summary>
    /// Notifies views whose membership changed in this contact's hover chain.
    /// </summary>
    private static void NotifyHoverChainChanges(List<View> previousChain, List<View> currentChain, PointerEvent ev)
    {
        foreach (var view in previousChain.Where(view => !currentChain.Contains(view)))
        {
            var exitedEvent = new PointerEvent(PointerEventKind.Leave) { Contact = ev.Contact };
            view.GetProperty(Panel.PointerExited)?.Invoke(view, exitedEvent);
        }

        foreach (var view in currentChain.Where(view => !previousChain.Contains(view)).Reverse())
        {
            var enteredEvent = new PointerEvent(PointerEventKind.Enter) { Contact = ev.Contact };
            view.GetProperty(Panel.PointerEntered)?.Invoke(view, enteredEvent);
        }
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
                state.CurrentPressChain.Contains(view) || state.CapturedView == view);

        view.SetProperty(property, value);
    }

    private void RemoveContactState(PointerContact contact, PointerContactState state)
    {
        var capturedView = state.CapturedView;
        var affectedViews = state.Reset();
        _contactStates.Remove(new PointerContactKey(contact.Device, contact.Id));

        foreach (var view in affectedViews)
            RecomputeViewProperty(view, Panel.IsPointerOver);
        foreach (var view in affectedViews)
            RecomputeViewProperty(view, Panel.IsPressed);

        NotifyPointerCaptureLost(capturedView);
    }

    private bool CancelPointerCapture(PointerContactState state)
    {
        var capturedView = state.CapturedView;
        if (capturedView is null)
            return false;

        var released = state.Release(capturedView);
        Debug.Assert(released, "A captured view selected for subtree cancellation must still be captured.");

        var affectedViews = new[] { capturedView }
            .Concat(state.CancelPress())
            .Distinct()
            .ToList();

        foreach (var view in affectedViews)
            RecomputeViewProperty(view, Panel.IsPressed);

        // Notify only after ownership and pressed state have been updated so handlers observe the final state.
        NotifyPointerCaptureLost(capturedView);
        return true;
    }

    private static void NotifyPointerCaptureLost(View? capturedView)
    {
        capturedView?.GetProperty(Panel.PointerCaptureLost)?.Invoke(capturedView, EventArgs.Empty);
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

        if (ev.Cancelled)
            return;

        PropertyKey<EventHandler<PointerEvent>> property;
        switch (ev.Kind)
        {
            case PointerEventKind.Move: property = Panel.PreviewPointerMove; break;
            case PointerEventKind.Down: property = Panel.PreviewPointerDown; break;
            case PointerEventKind.Up: property = Panel.PreviewPointerUp; break;
            default: return;
        }

        // Preview
        for (int i = views.Count - 1; i >= 0; i--)
        {
            if (ev.Handled || ev.Cancelled)
                return;

            var view = views[i];
            view.GetProperty(property)?.Invoke(view, ev);
        }

        if (ev.Handled || ev.Cancelled)
            return;

        switch (ev.Kind)
        {
            case PointerEventKind.Move: property = Panel.PointerMove; break;
            case PointerEventKind.Down: property = Panel.PointerDown; break;
            case PointerEventKind.Up: property = Panel.PointerUp; break;
            default: return;
        }

        // Bubble
        foreach (var view in views)
        {
            if (ev.Handled || ev.Cancelled)
                return;

            view.GetProperty(property)?.Invoke(view, ev);
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
        return _contactStates.Values.Any(state => state.CapturedView == view);
    }

    internal void SetIsPointerCapture(View view, bool capture)
    {
        Debug.WriteLine($"Capture {capture}");
        if (_activeContactState is not PointerContactState state)
            return;

        if (capture)
        {
            // TBD: Throw if not in pointer down
            if (state.Capture(view, out var previousOwner))
            {
                if (previousOwner is not null)
                {
                    var affectedViews = state.CancelPress()
                        .Append(previousOwner)
                        .Distinct();
                    foreach (var affectedView in affectedViews)
                        RecomputeViewProperty(affectedView, Panel.IsPressed);
                }

                view.SetProperty(Panel.IsPressed, true);
                if (previousOwner is not null)
                    NotifyPointerCaptureLost(previousOwner);
            }
        }
        if (!capture)
        {
            if (state.Release(view))
            {
                RecomputeViewProperty(view, Panel.IsPressed);
                NotifyPointerCaptureLost(view);
            }
        }
    }

    private void ClearPointerCapture(PointerContactState state)
    {
        Debug.WriteLine("CaptureLost");
        var capturedView = state.ReleaseCapture();
        if (capturedView is null)
            return;

        RecomputeViewProperty(capturedView, Panel.IsPressed);
        NotifyPointerCaptureLost(capturedView);
    }


}
