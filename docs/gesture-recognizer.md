# Gesture recognizers

## Status

ZurfurGui now has a shared gesture-recognition layer over the raw pointer router. The first gesture using
it is `ClickRecognizer`, including its optional long-click callback. The layer is intended to arbitrate
competing gestures per pointer contact; pan and flick recognition have not been implemented.

This document describes the current implementation and records the proposed next phase. Implemented
behavior and future design are called out separately so proposals are not mistaken for existing API.

## Design overview

Platform adapters deliver normalized low-level pointer input to `PointerRouter`. The router hit-tests
and routes raw `PointerEvent`s (preview and bubble), and for each uncaptured pointer-down it also starts
a gesture arena. A recognizer attached to a view takes part in that arena to claim the interaction, such
as a click or, in the future, a pan.

### The gesture arena

The gesture arena decides which gesture owns a pointer contact. A contact is one finger, pen, or mouse pointer from
the moment it goes down until it goes up or is canceled. The arena exists because one touch can mean several things:
pressing a button inside a scrolling list might be a click or the start of a pan, and nothing is known when the
finger lands. The arena lets every interested recognizer watch the same contact and settle the question as more
input arrives.

How it works:

1. When a contact goes down and is not already captured, the router starts an arena for it. The arena
   walks from the hit view up to the root and asks each recognizer attached along the way to create a
   session. A session is that recognizer's private state for this one contact. The sessions are the
   candidates.
2. While the contact is down, every candidate sees copies of the down, move, and up events. Each stays
   pending until it calls `Accept()` (this is my gesture) or `Reject()` (this is not my gesture). A click,
   for example, waits for pointer-up and accepts only if the pointer is released inside its view.
3. The first candidate to accept wins and the others are canceled. If the contact is still down, the
   winner's view receives exclusive logical capture, so it keeps getting the contact's input even after
   the pointer leaves it. If the winner accepts on pointer-up there is nothing left to capture, but it
   still wins and performs its action.
4. If every candidate rejects, the arena ends with no winner and only raw pointer events were delivered.
5. The arena ends when the contact goes up, the platform cancels it, or the winner is decided and
   finishes. Each contact has its own arena, and a new pointer-down starts a new one.

For example, a button sits inside a pannable list. The button's click recognizer and the list's pan
recognizer are both candidates on pointer-down. If the finger lifts in place, the click accepts and
wins. If the finger moves past the pan threshold first, the pan accepts, the click candidate is
canceled, and the list captures the contact.

Things that are useful to know:

- A recognizer must accept explicitly. The last remaining candidate does not win just because the
  others rejected.
- When more than one candidate accepts on the same input, the one closest to the hit view goes first.
- The arena runs alongside raw events. Preview and bubble routing continue as usual while candidates are
  pending. A raw handler that calls `View.SetCapture` cancels the arena: raw capture wins over pending
  gesture recognition.
- When a gesture wins before pointer-up, views that were pressed under the contact are sent
  `PointerCaptureLost`. If it wins during the pointer-down, that down is routed to the winner's view
  instead of the original hit chain.
- A candidate is canceled when the platform cancels the contact, an uncaptured contact leaves, its
  recognizer is removed from the view, its view is removed from the tree or becomes hidden, or another
  candidate wins. `OnCancelled` is not called for a winner that completes normally on pointer-up.
- Each candidate gets its own copy of the event. Setting `Handled` on it does not affect raw routing or another
  candidate.
- If recognizer code throws, the arena is canceled (a winner's capture is released) and the exception
  is rethrown to the router's caller. A recognizer may call `Accept()` or `Reject()` from inside
  `CreateSession`; the arena applies it right after the session receives its pointer-down.
- Gesture APIs are UI-thread only. Call `Accept` and `Reject` from the UI thread, for example from a
  `PreRenderFrame` callback.

Processing order matters: the arena starts and sends pointer-down to candidates before raw down routing.
For move and up, raw routing happens first, then the arena. A raw handler can therefore request capture
before a pending recognizer sees that move or release.

Known gaps (tracked in `src/ZurfurGui/Input/gestures-cleanup.md`): the arena is single-contact, so no
recognizer can use two contacts; there is no shared clock or event timestamp; and there is no wheel event.

## Public API

### `IGestureRecognizer` and `IGestureRecognizerSession`

Implement `IGestureRecognizer` to provide a gesture attached to a view. Its `CreateSession` method
receives a `GestureSessionContext` and returns independent state for that recognizer/contact candidate.
Implement `IGestureRecognizerSession` for that contact-specific state and respond to the pointer-down,
move, up, and cancellation callbacks. The session interface does not expose a view; use the context's
`CandidateView` for the candidate view.

Attach a recognizer with `View.AddGestureRecognizer`, which returns the recognizer, and detach it with
`View.RemoveGestureRecognizer`. A recognizer is considered for every contact whose hit path includes a
view it is attached to. Removing a recognizer cancels its live sessions: a pending session is rejected,
and a winner cancels the arena and releases its capture. The exception is a winner that is receiving
pointer-up, such as a click handler that removes its own recognizer; it is already completing normally.
Recognizers own nothing that needs disposal, so `IGestureRecognizer` is not `IDisposable`. A
recognizer and its view normally share a lifetime, so detaching is only needed to stop recognition early.

### Attached and candidate views

The attached view is the view a recognizer was added to. The recognizer does not know it. The candidate
view is the view for one recognizer/contact attempt, found by the arena as it walks the hit chain, and
it is exposed as `GestureSessionContext.CandidateView`. They are the same view; a recognizer instance
added to more than one view gets a separate candidate for each of those views in the hit chain. If a
session accepts before pointer-up, its candidate view receives the capture.

The arena gives each candidate a `GestureSessionContext` with the `CandidateView`, the initial `Contact`
snapshot, the derived `ContactKey`, and the `Accept()` and `Reject()` operations. The context becomes
inactive when the session completes or is canceled, so later calls have no effect. It can be used from
a timer callback while the contact is active. Later pointer updates arrive as the `PointerEvent` copies
passed to the session (kind, contact, and cancellation flag; `Handled` is independent of raw routed
events).

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

Use `_context.CandidateView` to access the candidate view and `_context.Contact` for the initial
contact snapshot.

### `ClickRecognizer`

`ClickRecognizer` is the first concrete `IGestureRecognizer`. It is used by `Button`, `ComboBox`,
`Scrim`, and other controls. Construct it with the callback and add it to the view that owns the
interaction. The view owns the recognizer, so the control does not keep a reference or remove it when it
detaches; a control that is moved to a new parent keeps working:

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

A click is accepted on pointer-up if the contact is not canceled and its device position is inside the
attached view's `OriginRect`. It remains a candidate while down; it does not capture or mark raw pointer
events handled while pending. Thus raw pointer handlers, including handlers on ancestors, continue to
receive events unless they handle them or request direct capture. A click callback runs after the raw
pointer-up route for that input.

The optional long-click callback uses `AppWindow.Properties.LongClickDuration` and
`LongClickMovementTolerance`. Moving farther than the tolerance stops long-click eligibility but does
not by itself reject the ordinary click. If the long-click callback sets
`PointerLongClickEventArgs.Handled`, the session accepts and captures its view and suppresses the click
on release. If the callback calls `SetCapture` on another view, that direct capture preempts the arena
and cancels the click session.

