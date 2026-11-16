# Input and pointer events

## Overview

This document describes the current high-level path for mouse and pointer input through ZurfurGui. It
starts after platform or browser input has been received; operating-system input processing is outside
the GUI layer.

The main processing phases are:

1. **Find the hit target and build the view chain.** Hit testing identifies the target and its ancestors,
   unless the contact is captured.
2. **Preview routing.** Uncaptured pointer events travel from the root toward the hit target.
3. **Bubble routing.** Uncaptured pointer events travel from the hit target back toward the root unless
   preview handling stopped the route.
4. **Action and state updates.** The system completes gestures and updates hover, pressed, and capture
   state, including cleanup on release or cancellation.

## Public API and behavior

### Common pointer events

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
- Platform enter can cause control enters if the hover chain gains views. Platform leave exits the
  chain's current members.
- The router updates hover state and `IsPointerOver` before invoking enter/exit handlers.

Register handlers with `View.AddEvent(Panel.PointerEntered, handler)` and
`View.AddEvent(Panel.PointerExited, handler)`. These direct notifications do not use `Handled` to stop
other views from being notified. `Handled` and `Cancelled` affect routed down, move, and up dispatch;
they do not undo a hover-chain change that has already been applied.

### Pointer state and capture

The router tracks the current hover target independently from pointer capture:

- `IsPointerOver` is true when at least one tracked contact's hover chain contains the view, unless any
  mouse button is down or a touch contact is active. In those cases it is false for every view.
- `IsPressed` represents the active press relationship.
- Capture lets a control continue receiving pointer events after the pointer leaves its normal bounds.

Each contact has independent hover, press, and capture state, with at most one capture owner. An
uncaptured event routes through its current hit chain. When capture is active, only the owner receives
the event; it is not also sent through the current hit chain. A new capture request replaces the previous
owner and raises `PointerCaptureLost` on that view. The previous press is cancelled, preventing affected
views from remaining pressed or activating on pointer-up; `PointerCaptureLost` is also raised on those
views, except the new capture owner. The normal hit test still runs, but `IsPointerOver` remains false
while any mouse button is down or a touch contact is active.

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

Gesture recognizers observe contacts alongside raw pointer routing. Pending recognition is not capture;
an accepted recognizer can receive exclusive logical capture. See
[Gesture Recognizers](gesture-recognizer.md) for the arena and recognizer behavior.

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

### Clicks and gestures

The router does not synthesize a generic click event. Controls use `ClickRecognizer` or another gesture
recognizer for higher-level actions. See [Gesture Recognizers](gesture-recognizer.md) for click, long-click,
pan, and custom recognizer behavior.

### Wheel events

Mouse wheel input is separate from pointer events. It does not use capture, hover, press, or the gesture
arena, and has no preview or bubble phase.

Views can handle wheel input with `Panel.PointerWheel`. If a handler leaves `WheelEvent.Handled` false,
the event continues toward parent views. `ScrollViewer` sets `Handled` whenever it can scroll in the
wheel's direction (even at the end of its range), so a wheel only reaches a parent scroller when the
child cannot scroll that way.

Deltas use the browser's pixel-mode convention. One detent of a typical mouse wheel is 100 units
(`WheelEvent.UnitsPerNotch`), positive `DeltaY` scrolls down, and positive `DeltaX` scrolls right.

### Timestamps

`PointerEvent.Timestamp` is the event time in milliseconds from a monotonic clock. Only differences
between events of one contact are meaningful. Recognizers use it to measure velocity.

<!-- INTERNAL -->

## Implementation Section

### Input ingress

Supported input adapters convert platform notifications into logical pointer events. The renderer
connects the input callback to the pointer router, which updates pointer state and dispatches the events
described above.

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
can complete click recognition after raw routing. See [Gesture Recognizers](gesture-recognizer.md) for
recognition timing and [Pointer state and capture](#pointer-state-and-capture) for cleanup behavior.

### Phase 1: Find the hit target and build the view chain

This phase searches from the app-window root and produces the hit chain used for uncaptured routing.
The chain contains the target and its ancestors up to the root. If no view is hit, the chain is empty.
Captured input instead routes through the capture owner.

For uncaptured input, the hit chain is consumed by the preview and bubble phases. It is also used to
update hover and pressed state and to notify views when a contact enters or exits the chain. A
pointer-down chain is retained for the active press relationship.

The search proceeds as follows:

1. A view is rejected if the point is outside its current bounds or if it is not visible.
2. Children are searched before their parent. Children are considered from front to back, so the
   topmost matching child wins.
3. A view with `HitTestMode.Disabled` is skipped for its own hit test.
4. The view's hit-test mode determines whether the view itself can be selected.
5. In `Normal` mode, a control-specific renderer can identify the rendered control area.
6. In `Normal` mode, a panel fallback checks the view's bounds and whether its background is
   sufficiently visible.

Every view has a `HitTestMode`, which defaults to `Normal`:

- `Normal` first calls `Renderable.IsHit` when the view has a control-specific renderer. If it does not
  report a hit, the panel fallback checks whether the background alpha exceeds the current threshold
  (`A > 16`).
- `Disabled` prevents the view itself from being selected. Its children are still searched, so this
  mode can be used for a layout or decorative wrapper that should not become the target.
- `Always` selects the view whenever the pointer is inside its arranged bounds. It does not call
  `IsHit`, because the mode explicitly makes the complete bounds interactive. Children are still
  searched first.

`IsHit` runs only for a visible view in `Normal` mode, after children fail and before the panel
background-alpha fallback. Renderers can define hit areas without a visible panel background, as with
text. The alpha fallback keeps clear panels from becoming input surfaces solely because they occupy
layout space.

The window title drag surface uses `Always` because it is intentionally transparent but must receive
events across the full title-bar area. This changes which view becomes the hit target; it does not
change preview or bubble routing.

### Phase 2: Preview event routing

For uncaptured input, the preview phase dispatches the event through the hit chain from root toward the
hit target. These are the `PreviewPointer...` events. Ancestors receive preview first.

`PointerEvent.Handled` stops normal routing at the current route boundary. When preview handling sets
`Handled`, the corresponding bubble route is skipped. `PointerEvent.Cancelled` stops normal routing for
the current event. It does not by itself release capture; platform pointer-cancel cleanup or explicit
capture release performs that state cleanup.

### Phase 3: Bubble event routing

For uncaptured input, the bubble phase uses the same hit chain built in Phase 1 to dispatch the pointer
event from the hit target back toward the root. These are the ordinary `Pointer...` events. Captured
input routes only to its owner.

This is fixed preview/bubble traversal, not a full WPF-style event system. Handled stops routing between
views, but combined handlers on one view are still invoked together.

### Phase 4: Action and state updates

State updates are interleaved with dispatch. The capture owner receives down, move, and up before hit
testing; hit testing then updates hover. Without capture, hit testing updates hover and press before
preview/bubble dispatch. Pointer-up clears the press relationship after dispatch. An uncaptured leave
clears hover and removes the contact. A platform cancel cancels capture and press state, then clears
hover and removes the contact even when capture was active. Platform enter can start the hover chain.

Geometry or view-tree changes alone do not trigger a new hit test; a later pointer input is required to
discover a changed target.

### Platform-specific wheel and timestamp handling

Platform adapters report an `OsWheelEvent` through `OsCanvas.WheelInput`, and the renderer connects it
to `PointerRouter.WheelInput`. The router hit-tests the pointer position, then walks from the hit view
toward the root and gives the `WheelEvent` to each view with a `Panel.PointerWheel` handler in turn.
`Handled` stops the walk; if it remains false, the event continues toward the root.

The browser adapter converts line and page deltas to `WheelEvent.UnitsPerNotch`. The WinForms adapter
converts from 120 units per detent, flips the vertical sign, and reads horizontal wheel input from the
native `WM_MOUSEHWHEEL` message. Neither adapter forwards Ctrl+wheel, because the browser and the
WinForms form use it for magnification.

The browser adapter forwards the DOM event's `timeStamp`; the WinForms touch and pen adapter forwards
the native pointer message time. Where a platform supplies no timestamp (WinForms mouse), the router
stamps the event on arrival.

The browser canvas sets `touch-action: none` so touch drags are delivered to the GUI instead of
becoming native browser scrolling (which would end the contact with a pointer cancel). It suppresses
the browser context menu so the right mouse button can be used by the GUI, for example to pan.

