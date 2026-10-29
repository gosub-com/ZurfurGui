# Input and pointer events

This document describes the current high-level path for mouse and pointer input through ZurfurGui. It
starts after platform or browser input has been received; operating-system input processing is outside
the GUI layer.

## Input ingress

Supported input adapters convert platform notifications into logical pointer events. The renderer
connects the input callback to the pointer router, which updates pointer state and dispatches the events
described below.

The **browser** adapter listens on the canvas for pointer enter, move, leave, down, up, and cancel. On
pointer-down it requests DOM pointer capture so subsequent events continue to reach the canvas if the
pointer moves outside it. The browser releases DOM capture on pointer-up or cancel. The adapter converts
browser client positions to canvas device pixels using the canvas backing size and client rectangle,
then forwards events through `OsCanvas.PointerInput`.

The **WinForms** adapter listens to the canvas control's mouse move, down, up, and leave notifications
and forwards the control coordinates through that same callback. It also observes native Windows
pointer messages so touch and pen contacts retain their device kind and contact ID. Mouse events remain
the fallback for ordinary mouse input.  

The platform differences end at this boundary. The GUI does not need to know whether an event
originated in JavaScript, native Windows messages, or C# interop.  

Browser DOM pointer capture ensures events reach the canvas; it is separate from the GUI's logical
capture, which a control requests through `View.SetCapture`. Both paths allow the shared router to
receive input and update hit-tested hover state while routing captured down, move, and up events to the
logical capture owner.

The renderer installs `PointerOver` as the callback target. Rendering and input therefore use the same
app-window view tree and the same device-coordinate space.

```text
Browser pointer events       WinForms mouse/pointer events
          |                            |
          +----------> shared OsPointerEvent callback
                                   |
                                   v
                         PointerOver processes event
                                   |
             Route move/down/up events to the capture owner
                                   |
                                   v
              1. Find target and build view chain
                                   |
                                   v
                  Update hover and press state
                     |                      |
                     |                      +-> PointerEntered / PointerExited
                     |                          direct notifications for chain changes
                     |
                     +-> If uncaptured, move/down/up:
                     |      2. Preview: root -> target
                     |                  |
                     |                  v
                     |      3. Bubble: target -> root
                     |                  |
                     |      pointer-up may invoke a click recognizer
                     |                  |
                     v                  v
                4. Complete state cleanup
                     |                  |
                     +-> pointer-up clears press state
                     +-> leave/cancel clears hover
```

The capture owner receives down, move, and up before hit testing. The router still hit-tests during
capture to update hover state, but does not route those events through the hit chain. A platform cancel
releases capture, clears press, hover, and contact state, and is not preview/bubble routed. A pointer-up
can invoke a click recognizer during its bubble route, before the router clears press state. See
[Pointer state and capture](#pointer-state-and-capture) for details.

## Phase 1: Find the hit target and build the view chain

This phase searches from the app-window root and produces the hit chain used for uncaptured routing.
The chain contains the target and its ancestors up to the root. If no view is hit, the chain is empty.
Captured input instead routes through the capture owner.

For uncaptured input, the hit chain is consumed by the preview and bubble phases. It is also used to
update hover and pressed state and to notify views when a contact enters or exits the chain. A
pointer-down chain is retained for the active press relationship.

The search proceeds as follows:

1. A view is rejected if the point is outside its current bounds or if it is not visible.
2. Children are searched before their parent. Children are considered from front to back, so the topmost matching
   child wins.
3. A view with `HitTestMode.Disabled` is skipped for its own hit test.
4. The view's hit-test mode determines whether the view itself can be selected.
5. In `Normal` mode, a control-specific renderer can identify the rendered control area.
6. In `Normal` mode, a panel fallback checks the view's bounds and whether its background is sufficiently visible.

### Hit-test modes and `IsHit`

Every view has a `HitTestMode`, which defaults to `Normal`:

- `Normal` first calls `Renderable.IsHit` when the view has a control-specific renderer. If it does not
  report a hit, the panel fallback checks whether the background alpha exceeds the current threshold
  (`A > 16`).
- `Disabled` prevents the view itself from being selected. Its children are still searched, so this mode can be
  used for a layout or decorative wrapper that should not become the target.
- `Always` selects the view whenever the pointer is inside its arranged bounds. It does not call `IsHit`,
  because the mode explicitly makes the complete bounds interactive. Children are still searched first.

`IsHit` runs only for a visible view in `Normal` mode, after children fail and before the panel
background-alpha fallback. Renderers can define hit areas without a visible panel background, as with
text. The alpha fallback keeps clear panels from becoming input surfaces solely because they occupy
layout space.

The window title drag surface uses `Always` because it is intentionally transparent but must receive
events across the full title-bar area. This changes which view becomes the hit target; it does not
change preview or bubble routing.


## Phase 2: Preview event routing

For uncaptured input, the preview phase dispatches the event through the hit chain from root toward the
hit target. These are the `PreviewPointer...` events. Ancestors receive preview first.

`PointerEvent.Handled` stops normal routing at the current route boundary. When preview handling sets
`Handled`, the corresponding bubble route is skipped. `PointerEvent.Cancelled` stops normal routing for
the current event. It does not by itself release capture; platform pointer-cancel cleanup or explicit
capture release performs that state cleanup.

## Phase 3: Bubble event routing

For uncaptured input, the bubble phase uses the same hit chain built in Phase 1 to dispatch the pointer
event from the hit target back toward the root. These are the ordinary `Pointer...` events. Captured
input routes only to its owner.

This is fixed preview/bubble traversal, not a full WPF-style event system. Handled stops routing between
views, but combined handlers on one view are still invoked together.

## Common pointer events

| Event | Delivery | When it is raised |
| --- | --- | --- |
| `PreviewPointerDown` | Routed, root to target | A pointer-down input is processed. |
| `PointerDown` | Routed, target to root | After preview unless handled or cancelled. |
| `PreviewPointerMove` | Routed, root to target | A pointer-move input is processed. |
| `PointerMove` | Routed, target to root | After preview unless handled or cancelled. |
| `PreviewPointerUp` | Routed, root to target | A pointer-up input is processed. |
| `PointerUp` | Routed, target to root | After preview unless handled or cancelled. |
| `PointerEntered` | Direct to each entering view | Its per-contact hover-chain membership begins. |
| `PointerExited` | Direct to each exiting view | Its per-contact hover-chain membership ends. |
| `PointerCaptureLost` | Direct to affected views | Capture ends or a gesture cancels their press; carries the contact key. |

For down, move, and up, the hit chain is used unless the contact is captured; then only its capture owner
receives the event. Captured contacts are still hit-tested for hover. Enter and exit are not routed or
redirected by capture.

`Panel.PointerEntered` and `Panel.PointerExited` are direct per-view notifications, not preview/bubble
routed events. They are raised when the router updates the hover chain and finds changed membership.
This normally occurs when the hit view changes; down and up also refresh the chain:

- A view receives `PointerEntered` when it joins that contact's hover chain. Entries are notified from ancestor to target.
- A view receives `PointerExited` when it leaves that contact's hover chain. Exits are notified from target to ancestor.
- Views shared by the old and new chains receive neither notification.
- The event's `Kind` is `Enter` or `Leave`, respectively. Both carry the `PointerContact` from the
  input that caused the chain change.
- The contact includes device, ID, position, dimensions, buttons, and modifiers. On platform leave, position fields
  may be outside the canvas or unsuitable as an in-view position.
- These notifications are independent of capture. A captured contact still updates its hit-tested hover chain.
- Platform enter and leave are hit-test inputs, not direct control events. Platform enter can cause
  control enters if the chain gains views. Platform leave exits the chain's current members.
- The router updates hover state and `IsPointerOver` before invoking enter/exit handlers. Geometry or
  view-tree changes alone do not trigger a new hit test; a later pointer input is required to discover a
  changed target.

Register handlers with `View.AddEvent(Panel.PointerEntered, handler)` and
View.AddEvent(Panel.PointerExited, handler)`. These direct notifications do not use `Handled` to stop
other views from being notified. `Handled` and `Cancelled` affect routed down, move, and up dispatch;
they do not undo a hover-chain change that has already been applied.

## Phase 4: Action and state updates

State updates are interleaved with dispatch. The capture owner receives down, move, and up before hit
testing; hit testing then updates hover. Without capture, hit testing updates hover and press before
preview/bubble dispatch. Pointer-up clears the press relationship after dispatch. An uncaptured leave
clears hover and removes the contact. A platform cancel cancels capture and press state, then clears
hover and removes the contact even when capture was active. Platform enter can start the hover chain.

### Pointer state and capture

The router tracks the current hover target independently from pointer capture:

- `IsPointerOver` is true when at least one tracked contact's hover chain contains the view.
- `IsPressed` represents the active press relationship.
- Capture lets a control continue receiving pointer events after the pointer leaves its normal bounds.

Each contact has independent hover, press, and capture state, with at most one capture owner. An
uncaptured event routes through its current hit chain. When capture is active, only the owner receives
the event; it is not also sent through the current hit chain. A new capture request replaces the previous
owner and raises `PointerCaptureLost` on that view. The previous press is cancelled, preventing affected
views from remaining pressed or activating on pointer-up; `PointerCaptureLost` is also raised on those
views, except the new capture owner. The normal hit test still runs, so `IsPointerOver` can move to
another view while the owner remains pressed.

While capture is active:

- Down, move, and up events are routed only to the capture owner. Ancestors are not added automatically.
- The owner receives pointer-up even if the pointer has moved outside its bounds.
- A pointer leave changes hover state but does not release capture.
- A platform pointer cancel uses the capture/press cleanup path, not normal preview/bubble routing. The
  capture owner receives `PointerCaptureLost`, and the contact's hover and state are removed.

Without capture, `IsPressed` is true only for views shared by the current hover chain and the press chain
recorded on pointer-down. It clears for a view when the pointer leaves that chain and becomes true again
if the pointer returns while the press is still active. The capture owner remains pressed while capture
is active, even outside that chain.

Controls that start a drag or similar interaction generally capture during pointer-down and use capture
loss to clean up. This is important because a release can occur outside the original target, and capture
can end without a normal pointer-up reaching that control.

Gesture recognizers use a separate arbitration step. On pointer-down, the router starts a per-contact
gesture session and collects recognizers attached to the hit view and its ancestors. Pending recognizers
receive that contact's input even if hit testing later selects a different view. Raw pointer-event
handling does not suppress their input. Pending recognition is not view capture: no view owns the
contact until a recognizer accepts. The winner then receives exclusive logical capture if it accepts
before the contact ends; a gesture accepted on pointer-up has no remaining stream to capture. Losing
recognizers are canceled. A direct `SetCapture` request from a raw pointer handler preempts and cancels
the gesture session before assigning or transferring capture.

Hover and pressed pseudo-properties are updated as chains and capture state change. This allows styles
and controls to react to pointer state without requiring every control to maintain its own hover
bookkeeping.

### Using pointer capture

Pointer capture is for controls that must continue an interaction after the pointer leaves their normal
hit area. Typical examples are dragging a thumb, resizing a window, or implementing a gesture. A
control requests capture by calling its view's `SetCapture` method during its `PointerDown` handler,
passing the contact key:

```csharp
void OnPointerDown(object? sender, PointerEvent e)
{
    View.SetCapture(e.Contact.Key, true);
}
```

Release capture explicitly with `SetCapture` when the interaction ends early:

```csharp
View.SetCapture(e.Contact.Key, false);
```

Controls that capture should also handle `Panel.PointerCaptureLost`. Capture can end without a normal
pointer-up, for example when a contact is cancelled or another control takes over capture. View removal
currently requires the control's own detach cleanup; automatic router cleanup is future work. Use the
handler to clear drag and pressed state, and make the cleanup safe to run more than once:

```csharp
View.AddEvent(Panel.PointerCaptureLost, OnPointerCaptureLost);

void OnPointerCaptureLost(object? sender, PointerContactKey contactKey)
{
    isDragging = false;
    View.SetProperty(Panel.IsPressed, false);
}
```

When adding or changing a captured interaction, verify these transitions: press inside and release
inside, press and drag outside before release, pointer leave while captured, and capture loss without a
normal pointer-up event.

Capture is contact-aware: use `e.Contact.Key` to identify the contact. Setting capture for a key that is
not currently tracked throws; releasing an untracked key is a no-op. `ClearCaptures()` releases all
contacts owned by that view. Capture is not a general-purpose way to make a button clickable. Buttons
should normally use the higher-level `Click` event. Capture keeps low-level pointer events together for
an interaction, but it does not by itself make a click valid when the pointer is released outside the
original press target.

### Gesture recognition and click-like activation (`ClickRecognizer`)

The router does not synthesize or route a generic click event. Controls that need click-like activation
use the public `ClickRecognizer`, an `IGestureRecognizer` attached to the view that owns the interaction
with `View.AddGestureRecognizer`. Custom gesture types implement `IGestureRecognizer` and create
per-contact `IGestureRecognizerSession` instances. The arena supplies a `GestureSessionContext` so
sessions can accept or reject without exposing the arena; the first accepting session wins the contact's
arena. `View.RemoveGestureRecognizer` detaches a recognizer and cancels its active sessions.

The click recognizer remains a candidate from pointer-down and invokes its callback when the contact is
released inside the attached view's bounds, provided no competing gesture has won. It does not capture
the view while pending and does not mark raw pointer events handled. Raw routed events therefore continue
independently of click recognition. Pointer cancellation, losing the gesture arena, removing the
recognizer, and release outside the bounds cancel the click.

The recognizer can also receive an optional long-click callback. Its threshold and movement tolerance
are configured on `AppWindow` as `LongClickDuration` (`TimeSpan`) and
`LongClickMovementTolerance`; defaults are 500 milliseconds and 8 view-local logical units. The
defaults are supplied by the `input.long-click.duration` and `input.long-click.movement-tolerance` theme
variables. The duration theme value uses the standard JSON `TimeSpan` format; for example,
`"00:00:00.5"` represents 500 milliseconds.
The duration starts on pointer-down. Moving farther than the tolerance cancels long-click eligibility,
but leaves the ordinary click behavior unchanged. The callback receives the latest contact in
`PointerLongClickEventArgs`. Setting `Handled` suppresses the ordinary click on release; leaving it
false allows the normal click if release is inside. The duration and tolerance are read from the app
window while the recognizer is active, so changing them can affect the current press. This flag controls
recognizer behavior only and does not retroactively change routed pointer events already dispatched.

A long-click callback can explicitly take capture for another view with the contact key. This preempts
and cancels the pending gesture session, then transfers exclusive logical capture to the requested view:

```csharp
void OnLongClick(object? sender, PointerLongClickEventArgs e)
{
    textView.View.SetCapture(e.Contact.Key, true);
    e.Handled = true;
}
```

`AppWindow.zui` configures the app-wide defaults through theme tokens. Applications can change the
settings at runtime through `appWindow.Properties.LongClickDuration` and
`appWindow.Properties.LongClickMovementTolerance`.

This allows a `Button`, `Scrim`, `ComboBox`, or third-party control to choose its own higher-level action
without adding another generic routed event to `Panel`. A `Button` uses the recognizer to raise its
public `Click` event. ComboBox item hosts remain panels and attach a recognizer to each item host.

## Wheel events

Mouse wheel input is separate from pointer events. Platform adapters report an `OsWheelEvent` through
`OsCanvas.WheelInput`, and the renderer connects it to `PointerRouter.WheelInput`. Wheel input does not
use capture, hover, press, or the gesture arena, and has no preview or bubble phase.

The router hit-tests the pointer position, then walks from the hit view toward the root and gives the
`WheelEvent` to each view with a `Panel.PointerWheel` handler in turn. A handler consumes the wheel by
setting `WheelEvent.Handled`, which stops the walk. If it leaves `Handled` false, the event continues
to the next handler toward the root. `ScrollViewer` always sets `Handled`, so a wheel never reaches a
parent scroller.

Deltas use the browser's pixel-mode convention. One detent of a typical mouse wheel is 100 units
(`WheelEvent.UnitsPerNotch`), positive `DeltaY` scrolls down, and positive `DeltaX` scrolls right. The
browser adapter converts line and page deltas to that unit. The WinForms adapter converts from 120 units
per detent and flips the vertical sign, and reads horizontal wheel from the native `WM_MOUSEHWHEEL`
message. Neither adapter forwards Ctrl+wheel, because the browser and the WinForms form use it for
magnification.

## Current scope

This description focuses on responsibilities and ordering rather than exact event records, view-chain
representation, or rendering data structures. The input path is evolving, so those implementation details
are not yet part of the GUI input contract.

