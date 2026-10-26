# Gesture recognizers

## Status

ZurfurGui now has a shared gesture-recognition layer over the raw pointer router. The first gesture using it is
`ClickRecognizer`, including its optional long-click callback. The layer is intended to arbitrate competing
gestures per pointer contact; pan and flick recognition have not been implemented.

This document describes the current implementation and records the proposed next phase. Implemented behavior and
future design are called out separately so proposals are not mistaken for existing API.

## Design overview

Platform adapters deliver normalized low-level pointer input to `PointerRouter`. The router hit-tests and routes raw
`PointerEvent`s (preview and bubble), and for each uncaptured pointer-down it also starts a gesture arena. A
recognizer attached to a view takes part in that arena to claim the interaction, such as a click or, in the future,
a pan.

### The gesture arena

The gesture arena decides which gesture owns a pointer contact. A contact is one finger, pen, or mouse pointer from
the moment it goes down until it goes up or is canceled. The arena exists because one touch can mean several things:
pressing a button inside a scrolling list might be a click or the start of a pan, and nothing is known when the
finger lands. The arena lets every interested recognizer watch the same contact and settle the question as more
input arrives.

How it works:

1. When a contact goes down and is not already captured, the router starts an arena for it. The arena walks from the
   hit view up to the root and asks each recognizer attached along the way to create a session. A session is that
   recognizer's private state for this one contact. The sessions are the candidates.
2. While the contact is down, every candidate sees copies of the down, move, and up events. Each stays pending until
   it calls `Accept()` (this is my gesture) or `Reject()` (this is not my gesture). A click, for example, waits for
   pointer-up and accepts only if the pointer is released inside its view.
3. The first candidate to accept wins and the others are canceled. If the contact is still down, the winner's view
   receives exclusive logical capture, so it keeps getting the contact's input even after the pointer leaves it. If
   the winner accepts on pointer-up there is nothing left to capture, but it still wins and performs its action.
4. If every candidate rejects, the arena ends with no winner and only raw pointer events were delivered.
5. The arena ends when the contact goes up, the platform cancels it, or the winner is decided and finishes. Each
   contact has its own arena, and a new pointer-down starts a new one.

For example, a button sits inside a pannable list. The button's click recognizer and the list's pan recognizer are
both candidates on pointer-down. If the finger lifts in place, the click accepts and wins. If the finger moves past
the pan threshold first, the pan accepts, the click candidate is canceled, and the list captures the contact.

Things that are useful to know:

- A recognizer must accept explicitly. The last remaining candidate does not win just because the others rejected.
- When more than one candidate accepts on the same input, the one closest to the hit view goes first.
- The arena runs alongside raw events. Preview and bubble routing continue as usual while candidates are pending.
  A raw handler that calls `View.SetCapture` cancels the arena: raw capture wins over pending gesture recognition.
- When a gesture wins before pointer-up, views that were pressed under the contact are sent `PointerCaptureLost`.
  If it wins during the pointer-down, that down is routed to the winner's view instead of the original hit chain.
- A candidate is canceled when the platform cancels the contact, an uncaptured contact leaves, its recognizer is
  removed from the view, its view is removed from the tree or becomes hidden, or another candidate wins.
  `OnCancelled` is not called for a winner that completes normally on pointer-up.
- Each candidate gets its own copy of the event. Setting `Handled` on it does not affect raw routing or another
  candidate.
- If recognizer code throws, the arena is canceled (a winner's capture is released) and the exception is rethrown to
  the router's caller. A recognizer may call `Accept()` or `Reject()` from inside `CreateSession`; the arena applies
  it right after the session receives its pointer-down.
- Gesture APIs are UI-thread only. Call `Accept` and `Reject` from the UI thread, for example from a
  `PreRenderFrame` callback.

Processing order matters: the arena starts and sends pointer-down to candidates before raw down routing. For move and
up, raw routing happens first, then the arena. A raw handler can therefore request capture before a pending
recognizer sees that move or release.

Known gaps (tracked in `src/ZurfurGui/Input/gestures-cleanup.md`): the arena is single-contact, so no recognizer can
use two contacts; there is no shared clock or event timestamp; and there is no wheel event.

## Public API

### `IGestureRecognizer` and `IGestureRecognizerSession`

Implement `IGestureRecognizer` to provide a gesture attached to a view. Its `CreateSession` method receives a
`GestureSessionContext` and returns independent state for that recognizer/contact candidate. Implement
`IGestureRecognizerSession` for that contact-specific state and respond to the pointer-down, move, up, and cancellation
callbacks. The session interface does not expose a view; use the context's `CandidateView` for the candidate view.

Attach a recognizer with `View.AddGestureRecognizer`, which returns the recognizer, and detach it with
`View.RemoveGestureRecognizer`. A recognizer is considered for every contact whose hit path includes a view it is
attached to. Removing a recognizer cancels its live sessions: a pending session is rejected, and a winner cancels the
arena and releases its capture. The exception is a winner that is receiving pointer-up, such as a click handler that
removes its own recognizer; it is already completing normally. Recognizers own nothing that needs disposal, so
`IGestureRecognizer` is not `IDisposable`. A recognizer and its view normally share a lifetime, so detaching is only
needed to stop recognition early.

### Attached and candidate views

The attached view is the view a recognizer was added to. The recognizer does not know it. The candidate view is the
view for one recognizer/contact attempt, found by the arena as it walks the hit chain, and it is exposed as
`GestureSessionContext.CandidateView`. They are the same view; a recognizer instance added to more than one view gets
a separate candidate for each of those views in the hit chain. If a session accepts before pointer-up, its candidate
view receives the capture.

The arena gives each candidate a `GestureSessionContext` with the `CandidateView`, the initial `Contact` snapshot, the
derived `ContactKey`, and the `Accept()` and `Reject()` operations. The context becomes inactive when the session
completes or is canceled, so later calls have no effect. It can be used from a timer callback while the contact is
active. Later pointer updates arrive as the `PointerEvent` copies passed to the session (kind, contact, and
cancellation flag; `Handled` is independent of raw routed events).

A custom recognizer can use this general shape:

```csharp
sealed class ExampleRecognizer : IGestureRecognizer
{
    public IGestureRecognizerSession? CreateSession(GestureSessionContext context) =>
        new ExampleSession(context);
}

// Attach: view.AddGestureRecognizer(new ExampleRecognizer());

sealed class ExampleSession : IGestureRecognizerSession
{
    readonly GestureSessionContext _context;

    public ExampleSession(GestureSessionContext context)
    {
        _context = context;
    }

    public void OnPointerDown(PointerEvent e) { }
    public void OnPointerMove(PointerEvent e) { }

    public void OnPointerUp(PointerEvent e)
    {
        if (/* this interaction matched */)
            _context.Accept();
        else
            _context.Reject();
    }

    public void OnCancelled() { }
}
```

Use `_context.CandidateView` to access the candidate view and `_context.Contact` for the initial contact snapshot.

### `ClickRecognizer`

`ClickRecognizer` is the first concrete `IGestureRecognizer`. It is used by `Button`, `ComboBox`, `Scrim`,
and other controls. Construct it with the callback and add it to the view that owns the interaction. The view owns
the recognizer, so the control does not keep a reference or remove it when it detaches; a control that is moved to a
new parent keeps working:

```csharp
public ExampleControl()
{
    InitializeControl();
    View.AddGestureRecognizer(new ClickRecognizer(OnClick));
}

void OnClick(object? sender, PointerEvent e)
{
    // Perform the control action.
}
```

A click is accepted on pointer-up if the contact is not canceled and its device position is inside the attached view's
`OriginRect`. It remains a candidate while down; it does not capture or mark raw pointer events handled while pending.
Thus raw pointer handlers, including handlers on ancestors, continue to receive events unless they handle them or
request direct capture. A click callback runs after the raw pointer-up route for that input.

The optional long-click callback uses `AppWindow.Properties.LongClickDuration` and
`LongClickMovementTolerance`. Moving farther than the tolerance stops long-click eligibility but does not by itself
reject the ordinary click. If the long-click callback sets `PointerLongClickEventArgs.Handled`, the session accepts and
captures its view and suppresses the click on release. If the callback calls `SetCapture` on another view, that direct
capture preempts the arena and cancels the click session.

## Current limitations and compatibility notes

- There is no pan, pinch, swipe, or inertia recognizer yet.
- There is no public gesture-priority setting. Same-input acceptances are resolved in hit-target-to-root order.
- Only one logical view owns capture for a contact. Multiple recognizers may compete while pending, but multiple
  views do not simultaneously capture the same contact.
- Raw `PointerEvent` routing remains available for specialized controls. Standard controls should prefer recognizers
  for semantic interactions such as clicks and future pan gestures.
- `ClickRecognizer` behavior changed during migration: ordinary clicks no longer capture on pointer-down or
  set raw `PointerEvent.Handled`. Raw routes may therefore reach ancestors that previously did not receive those
  events. This is intentional and permits a parent pan recognizer to compete with a child click recognizer.
- A raw control that calls `SetCapture` on pointer-down preempts the arena. A parent pan cannot win over that control
  until the control participates through a gesture recognizer instead.
- The solution has no automated test project. The TestApp pointer-input surface logs click activity and can be used
  for interactive verification; a solution build is the current automated validation.

## TBD in the future

The next feature is drag panning with optional inertial flicking, initially for `ScrollViewer` and ultimately reusable
on any view. The design discussion and agreed direction are recorded here.

### Proposed pan behavior

1. A pan recognizer is attached to a view and participates in the existing per-contact arena.
2. On pointer-down it records a candidate, but does not accept or capture immediately. This lets a child button's click
   remain possible.
3. Once movement exceeds a device-appropriate threshold, pan accepts. The arena cancels the click candidate and gives
   logical capture to the pan view. The router continues delivering the captured stream after the pointer leaves the
   surface.
4. During movement, pan invokes a caller-supplied callback with incremental movement deltas. A general view can use
   the callback to update `Panel.VisualOffset`; the recognizer need not own the target's offset or bounds.
5. If inertia is enabled, recent movement samples estimate release velocity. After release, the recognizer reports
   decaying deltas over render frames until velocity is small or the gesture is stopped.

The callback should have a documented coordinate convention: a positive delta follows pointer movement and represents
how far the visual content should move. For `ScrollViewer`, the instantiating lambda should apply the opposite sign to
`ScrollOffset`, because a larger scroll offset moves the content in the opposite direction. `ScrollOffset` already
clamps to scrollbar ranges and synchronizes both scrollbar values.

A generic recognizer that writes `Panel.VisualOffset` directly is convenient for arbitrary views. However, directly
changing the ScrollViewer content window's visual offset would bypass its `_scrollOffset` field and scrollbar values.
For ScrollViewer, prefer a callback that updates `ScrollOffset`; alternatively, add explicit reverse synchronization
from `Panel.VisualOffset` if direct visual-offset attachment is required. No such reverse synchronization exists today.

### Mouse and touch policy

Touch should pan by default after the movement threshold. Mouse left-drag panning should not be assumed as the default:
it can conflict with text selection and ordinary control interaction. A configurable mouse policy could support disabled,
left-drag, middle-button, right-button, or modifier-plus-left-drag. Middle-button or a modifier avoids some selection
conflicts; right-button may conflict with context menus. The default mouse policy remains undecided.

The threshold should be device-aware and configurable or sourced from platform conventions. A button press below the
threshold should remain a click. Once pan accepts, the click session must be canceled and should no longer invoke its
callback. Current click behavior itself has no movement slop; it accepts when released inside, so the pan candidate is
responsible for winning after sufficient movement.

### Flick and bounds considerations

`PointerEvent` contains no timestamp. Velocity estimation must sample pointer positions with a clock such as `Stopwatch`.
`AppWindow.PreRenderFrame` provides a frame callback but no elapsed-time argument, so inertia needs its own elapsed-time
A flick should stop on a new contact, capture
loss, detachment of the target view, negligible velocity, or a boundary.

A simple `Action<Point>` callback can apply deltas, but it cannot report how much movement a bounded target actually
consumed. `ScrollOffset` clamps at the content edge, so an inertial recognizer using only `Action<Point>` may continue
running while the offset remains clamped. Decide whether to accept that short tail, add a callback result/continuation
signal, or give the ScrollViewer a specialized pan adapter that can stop at bounds.

Nested ScrollViewer handoff is not part of the first pan implementation. The current router has no nested-scroll
protocol. A child pan that reaches an edge will not automatically transfer remaining movement to an ancestor; define
that separately if nested panning is required.

### Framework comparison discussed

| Framework | Touch/scroll behavior | Gesture ownership and mouse notes |
|---|---|---|
| WPF | `ScrollViewer` supports touch panning through settings such as `PanningMode` and `PanningDeceleration`. | Mouse capture and touch capture are per input device/contact, not shared among views. Ordinary mouse drag is not the default scroll gesture. |
| Avalonia | `ScrollViewer` supports touch scrolling through gesture recognition; touch and wheel/trackpad input are distinct paths. | Pointer capture is per pointer, with capture-loss notification. A shared multi-view capture model is not the normal gesture mechanism. |
| Android | Scroll views use touch slop before treating movement as scrolling; `ViewGroup` interception can take over and send `ACTION_CANCEL` to a child. Fling uses tracked velocity. | `ViewConfiguration` provides platform touch slop. Nested-scrolling APIs let parents consume scroll deltas separately from pointer ownership. Mouse dragging is handled separately from touch scrolling. |

These are framework design patterns, not a requirement to copy undocumented internal thresholds. Check the target
framework/version documentation if implementation details become important.

### Decisions already made

- Keep one exclusive logical capture owner per pointer contact; do not allow multiple views to capture the same stream.
- Start an internal arena/session at down so candidates retain the stream and original hit path. This is not view capture.
- Pending recognizers observe input even when a raw pointer handler sets `Handled`.
- An explicit raw `SetCapture` preempts and cancels the arena before assigning capture.
- Keep raw pointer events as a lower-level escape hatch; use the shared recognizer API for standard semantic controls.
- Migrate click recognition first; implement pan and ScrollViewer hookup as a later phase.
- The pan callback can be a lambda receiving deltas; the instantiator applies them to the desired visual or scroll offset.

### Continuation checklist

1. Implement a per-contact `PanGestureRecognizer` session with movement threshold and configurable allowed axes.
2. Decide mouse-button policy and touch/mouse threshold defaults.
3. Add optional velocity sampling and frame-driven deceleration, with deterministic cleanup on all cancellation paths.
4. Connect a generic view by applying callback deltas to `Panel.VisualOffset`.
5. Connect ScrollViewer through `ScrollOffset` so clamping and scrollbar synchronization stay centralized.
6. Choose a boundary-stop contract for inertia and document the chosen behavior.
7. Exercise a surface containing both a Button and selectable text: tap/click should still work below threshold, pan should win after threshold.
8. Consider nested-scroll handoff separately; do not assume current capture routing provides it.
9. Before starting, finish the cleanup items marked necessary before pan/zoom in `gestures-cleanup.md`.
10. Add automated gesture tests
