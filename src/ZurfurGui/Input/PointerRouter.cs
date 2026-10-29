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
internal class PointerRouter
{
    readonly AppWindow _appWindow;
    readonly Dictionary<PointerContactKey, PointerContactState> _contactStates = new();

    internal IReadOnlyList<PointerContact> GetContacts()
    {
        return _contactStates.Values.Select(state => state.Contact).ToArray();
    }

    internal IReadOnlyList<CapturedPointerContact> GetCapturedContacts()
    {
        return _contactStates.Values
            .Where(state => state.CapturedView is not null)
            .Select(state => new CapturedPointerContact(state.Contact, state.CapturedView!))
            .ToArray();
    }

    public PointerRouter(AppWindow appWindow)
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

        var state = GetContactState(ev.Contact);
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

        if (ev.Kind == PointerEventKind.Down && !wasCaptured && state.CapturedView is null)
            state.GestureArena = GestureArena.Start(this, state, chain, ev);

        // A captured control remains pressed until capture is released, even after the pointer leaves its
        // original hover chain. This is required for drag operations and pressed visuals outside the control.
        state.CapturedView?.SetProperty(Panel.IsPressed, true);

        // Phases 2-3: A down that becomes captured during gesture-arena start routes to its new capture owner.
        // Other input that was captured on entry was already routed above.
        if (!wasCaptured)
        {
            if (state.CapturedView is View currentCaptureTarget)
                SendPointerEvent(ev, [currentCaptureTarget]);
            else
                SendPointerEvent(ev, chain);
        }

        if (state.GestureArena is GestureArena gestureArena && ev.Kind is PointerEventKind.Move or PointerEventKind.Up)
            gestureArena.Process(ev);

        // Phase 4: Finish state cleanup after routing. Cancel and uncaptured leave also remove the contact state.
        if (ev.Kind == PointerEventKind.Up)
        {
            if (state.CapturedView is not null)
                ClearPointerCapture(state);

            var previousPressChain = state.CurrentPressChain.ToList();
            state.SetPressChain(Array.Empty<View>());
            state.UpdateCurrentPressChain();
            UpdateViewChain(previousPressChain, state.CurrentPressChain, Panel.IsPressed);

            // A touch or pen contact ceases to exist on up, so do not depend on the platform sending a leave.
            // A mouse persists (hover) after up.
            if (ev.Contact.Device != PointerDeviceKind.Mouse)
            {
                var previousHoverChain = state.HoverChain.ToList();
                state.UpdateHover(null, GetViewChain(null));
                UpdateViewChain(previousHoverChain.ToList(), state.HoverChain, Panel.IsPointerOver);
                NotifyHoverChainChanges(previousHoverChain, state.HoverChain, ev);
                RemoveContactState(ev.Contact, state);
            }
        }
        else if (ev.Kind == PointerEventKind.Cancel || (ev.Kind == PointerEventKind.Leave && !wasCaptured))
        {
            state.GestureArena?.Cancel(preserveCapture: true);
            RemoveContactState(ev.Contact, state);
        }

    }

    /// <summary>
    /// Called when wheel input is received. Hit tests the pointer position and offers the event to each view
    /// with a PointerWheel handler, starting at the hit view and walking toward the root, until a handler
    /// sets WheelEvent.Handled. A ScrollViewer always sets it, so a wheel never escapes to a parent
    /// scroller. Wheel input
    /// ignores pointer capture and does not change hover, press, or gesture state.
    /// </summary>
    public void WheelInput(OsWheelEvent rawEvent)
    {
        var ev = rawEvent.ToWheelEvent();
        var hit = FindHitTarget(_appWindow.View, new HitTestContext(ev.Contact));
        foreach (var view in GetViewChain(hit))
        {
            var handler = view.TryGetProperty(Panel.PointerWheel);
            if (handler != null)
            {
                handler.Invoke(view, ev);
                if (ev.Handled)
                    return;
            }
        }
    }

    /// <summary>
    /// Notifies views whose membership changed in this contact's hover chain.
    /// </summary>
    private static void NotifyHoverChainChanges(List<View> previousChain, List<View> currentChain, PointerEvent ev)
    {
        foreach (var view in previousChain.Where(view => !currentChain.Contains(view)))
        {
            var exitedEvent = new PointerEvent(PointerEventKind.Leave) { Contact = ev.Contact };
            view.TryGetProperty(Panel.PointerExited)?.Invoke(view, exitedEvent);
        }

        foreach (var view in currentChain.Where(view => !previousChain.Contains(view)).Reverse())
        {
            var enteredEvent = new PointerEvent(PointerEventKind.Enter) { Contact = ev.Contact };
            view.TryGetProperty(Panel.PointerEntered)?.Invoke(view, enteredEvent);
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
        state.GestureArena?.Cancel(preserveCapture: true);
        var capturedView = state.CapturedView;
        var affectedViews = state.Reset();
        _contactStates.Remove(new PointerContactKey(contact.Device, contact.Id));

        foreach (var view in affectedViews)
            RecomputeViewProperty(view, Panel.IsPointerOver);
        foreach (var view in affectedViews)
            RecomputeViewProperty(view, Panel.IsPressed);

        NotifyPointerCaptureLost(capturedView, state.Contact.Key);
    }

    private bool CancelPointerCapture(PointerContactState state)
    {
        state.GestureArena?.Cancel(preserveCapture: true);
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
        NotifyPointerCaptureLost(capturedView, state.Contact.Key);
        return true;
    }

    private static void NotifyPointerCaptureLost(View? capturedView, PointerContactKey contactKey)
    {
        capturedView?.TryGetProperty(Panel.PointerCaptureLost)?.Invoke(capturedView, contactKey);
    }

    static View? FindHitTarget(View view, HitTestContext context)
    {
        // Quick exit when not visible or not in clip region
        var clip = view.OriginRect;
        if (!clip.Contains(context.Contact.DevicePosition))
            return null;

        if (!view.IsVisible)
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
        if (hitTest == HitTesting.Disabled)
            return null;

        if (hitTest == HitTesting.Always)
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
            view.TryGetProperty(property)?.Invoke(view, ev);
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

            view.TryGetProperty(property)?.Invoke(view, ev);
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

    internal void SetCapture(View view, PointerContactKey contactKey, bool capture)
    {
        if (!_contactStates.TryGetValue(contactKey, out var state))
        {
            if (capture)
                throw new InvalidOperationException($"Pointer contact '{contactKey}' is not active.");
            return;
        }

        if (capture)
        {
            state.GestureArena?.Cancel(preserveCapture: true);
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
                    NotifyPointerCaptureLost(previousOwner, state.Contact.Key);
            }
        }
        else if (state.CapturedView == view)
        {
            state.GestureArena?.Cancel(preserveCapture: true);
            if (state.Release(view))
            {
                RecomputeViewProperty(view, Panel.IsPressed);
                NotifyPointerCaptureLost(view, state.Contact.Key);
            }
        }
    }

    internal void CaptureFromGesture(PointerContactState state, View view)
    {
        var cancelledPressViews = state.CancelPress();
        var affectedViews = cancelledPressViews.Append(view).Distinct();
        foreach (var affectedView in affectedViews)
            RecomputeViewProperty(affectedView, Panel.IsPressed);

        if (state.Capture(view, out var previousOwner))
        {
            view.SetProperty(Panel.IsPressed, true);

            var notifiedViews = new HashSet<View>(cancelledPressViews.Where(cancelledView => cancelledView != view));
            if (previousOwner is not null && previousOwner != view)
                notifiedViews.Add(previousOwner);

            foreach (var notifiedView in notifiedViews)
                NotifyPointerCaptureLost(notifiedView, state.Contact.Key);
        }
    }

    internal void ReleaseGestureCapture(PointerContactState state, View view)
    {
        if (state.CapturedView == view)
            ClearPointerCapture(state);
    }

    internal void ClearCaptures(View view)
    {
        foreach (var state in _contactStates.Values.ToList())
        {
            if (state.CapturedView == view)
                state.GestureArena?.Cancel(preserveCapture: true);

            if (!state.Release(view))
                continue;

            RecomputeViewProperty(view, Panel.IsPressed);
            NotifyPointerCaptureLost(view, state.Contact.Key);
        }
    }

    private void ClearPointerCapture(PointerContactState state)
    {
        Debug.WriteLine("CaptureLost");
        var capturedView = state.ReleaseCapture();
        if (capturedView is null)
            return;

        RecomputeViewProperty(capturedView, Panel.IsPressed);
        NotifyPointerCaptureLost(capturedView, state.Contact.Key);
    }

    /// <summary>
    /// Cancels all pointer state that refers to a view or its descendants. Called while the view is still attached
    /// because it is leaving the view tree: gesture candidates, capture, and press and hover membership are dropped.
    /// </summary>
    internal void CancelViewTree(View root)
    {
        foreach (var state in _contactStates.Values.ToArray())
        {
            state.GestureArena?.CancelViewTree(root);

            if (state.CapturedView is View captured && IsInViewTree(captured, root))
                ClearPointerCapture(state);

            var removed = state.HoverChain.Concat(state.PressChain).Concat(state.CurrentPressChain)
                .Where(view => IsInViewTree(view, root))
                .Distinct()
                .ToList();
            if (removed.Count == 0)
                continue;

            state.HoverChain.RemoveAll(view => IsInViewTree(view, root));
            state.PressChain.RemoveAll(view => IsInViewTree(view, root));
            state.CurrentPressChain.RemoveAll(view => IsInViewTree(view, root));
            if (state.HoverView is View hoverView && IsInViewTree(hoverView, root))
                state.HoverView = null;

            foreach (var view in removed)
            {
                RecomputeViewProperty(view, Panel.IsPointerOver);
                RecomputeViewProperty(view, Panel.IsPressed);
            }
        }
    }

    // Returns true if the view is the root or one of its descendants.
    internal static bool IsInViewTree(View view, View root)
    {
        for (View? current = view; current is not null; current = current.Parent)
        {
            if (current == root)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Cancels the live gesture sessions of a recognizer that was removed from a view.
    /// </summary>
    internal void CancelRecognizer(IGestureRecognizer recognizer, View view)
    {
        foreach (var state in _contactStates.Values.ToArray())
            state.GestureArena?.CancelRecognizer(recognizer, view);
    }
}
