# Input and pointer events

This document describes the current high-level path for mouse and pointer input through ZurfurGui. It starts after
platform or browser input has been received; operating-system input processing is outside the GUI layer.

## Input ingress

Supported input adapters convert platform notifications into logical pointer events. The renderer connects the
input callback to the pointer router, which updates pointer state and dispatches the events described below.

The **browser** adapter listens on the canvas for pointer enter, move, leave, down, and up. It converts browser client
positions to canvas device pixels and forwards events through `OsCanvas.PointerInput`.

The **WinForms** adapter listens to the canvas control's mouse move, down, up, and leave notifications and forwards the
control coordinates through that same callback. It also observes native Windows pointer messages so touch and pen
contacts retain their device kind and contact ID. Mouse events remain the fallback for ordinary mouse input. 

The platform differences end at this boundary. The GUI does not need to know whether an event originated in 
JavaScript, native Windows messages, or C# interop.

The renderer installs `PointerOver` as the callback target. Rendering and input therefore use the same app-window view
tree and the same device-coordinate space.

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

The capture owner receives down, move, and up before hit testing. The router still hit-tests during capture to update
hover state, but does not route those events through the hit chain. A platform cancel releases capture, clears press,
hover, and contact state, and is not preview/bubble routed. A pointer-up can invoke a click recognizer during its
bubble route, before the router clears press state. See
[Pointer state and capture](#pointer-state-and-capture) for details.

## Phase 1: Find the hit target and build the view chain

This phase searches from the app-window root and produces the hit chain used for uncaptured routing. The chain
contains the target and its ancestors up to the root. If no view is hit, the chain is empty. Captured input instead
routes through the capture owner.

For uncaptured input, the hit chain is consumed by the preview and bubble phases. It is also used to update hover and
pressed state and to notify views when a contact enters or exits the chain. A pointer-down chain is retained for the
active press relationship.

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

- `Normal` first calls `Renderable.IsHit` when the view has a control-specific renderer. If it does not report a
  hit, the panel fallback checks whether the background alpha exceeds the current threshold (`A > 16`).
- `Disabled` prevents the view itself from being selected. Its children are still searched, so this mode can be
  used for a layout or decorative wrapper that should not become the target.
- `Always` selects the view whenever the pointer is inside its arranged bounds. It does not call `IsHit`, because
  the mode explicitly makes the complete bounds interactive. Children are still searched first.

`IsHit` runs only for a visible view in `Normal` mode, after children fail and before the panel background-alpha
fallback. Renderers can define hit areas without a visible panel background, as with text. The alpha fallback keeps
clear panels from becoming input surfaces solely because they occupy layout space.

The window title drag surface uses `Always` because it is intentionally transparent but must receive events across the
full title-bar area. This changes which view becomes the hit target; it does not change preview or bubble routing.


## Phase 2: Preview event routing

For uncaptured input, the preview phase dispatches the event through the hit chain from root toward the hit target.
These are the `PreviewPointer...` events. Ancestors receive preview first.

`PointerEvent.Handled` stops normal routing at the current route boundary. When preview handling sets `Handled`, the
corresponding bubble route is skipped. `PointerEvent.Cancelled` stops normal routing for the current event. It does
not by itself release capture; platform pointer-cancel cleanup or explicit capture release performs that state cleanup.

## Phase 3: Bubble event routing

For uncaptured input, the bubble phase uses the same hit chain built in Phase 1 to dispatch the pointer event from the
hit target back toward the root. These are the ordinary `Pointer...` events. Captured input routes only to its owner.

This is fixed preview/bubble traversal, not a full WPF-style event system. Handled stops routing between views, but
combined handlers on one view are still invoked together.

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
| `PointerCaptureLost` | Direct to the released capture owner | Capture is released, replaced, or cancelled; carries the `PointerContactKey`. |

For down, move, and up, the hit chain is used unless the contact is captured; then only its capture owner receives
the event. Captured contacts are still hit-tested for hover. Enter and exit are not routed or redirected by capture.

`Panel.PointerEntered` and `Panel.PointerExited` are direct per-view notifications, not preview/bubble routed events.
They are raised when the router updates the hover chain and finds changed membership. This normally occurs when the
hit view changes; down and up also refresh the chain:

- A view receives `PointerEntered` when it joins that contact's hover chain. Entries are notified from ancestor to target.
- A view receives `PointerExited` when it leaves that contact's hover chain. Exits are notified from target to ancestor.
- Views shared by the old and new chains receive neither notification.
- The event's `Kind` is `Enter` or `Leave`, respectively. Both carry the `PointerContact` from the input that caused the
  chain change.
- The contact includes device, ID, position, dimensions, buttons, and modifiers. On platform leave, position fields
  may be outside the canvas or unsuitable as an in-view position.
- These notifications are independent of capture. A captured contact still updates its hit-tested hover chain.
- Platform enter and leave are hit-test inputs, not direct control events. Platform enter can cause control enters
  if the chain gains views. Platform leave exits the chain's current members.
- The router updates hover state and `IsPointerOver` before invoking enter/exit handlers. Geometry or view-tree changes
  alone do not trigger a new hit test; a later pointer input is required to discover a changed target.

Register handlers with `View.AddEvent(Panel.PointerEntered, handler)` and
`View.AddEvent(Panel.PointerExited, handler)`. These direct notifications do not use `Handled` to stop other views from
being notified. `Handled` and `Cancelled` affect routed down, move, and up dispatch; they do not undo a hover-chain
change that has already been applied.

## Phase 4: Action and state updates

State updates are interleaved with dispatch. The capture owner receives down, move, and up before hit testing; hit
testing then updates hover. Without capture, hit testing updates hover and press before preview/bubble dispatch. Pointer-up
clears the press relationship after dispatch. An uncaptured leave clears hover and removes the contact. A platform
cancel cancels capture and press state, then clears hover and removes the contact even when capture was active. Platform
enter can start the hover chain.

### Pointer state and capture

The router tracks the current hover target independently from pointer capture:

- `IsPointerOver` is true when at least one tracked contact's hover chain contains the view.
- `IsPressed` represents the active press relationship.
- Capture lets a control continue receiving pointer events after the pointer leaves its normal bounds.

Each contact has independent hover, press, and capture state, with at most one capture owner. An uncaptured event
routes through its current hit chain. When capture is active, only the owner receives the event; it is not also sent
through the current hit chain. A new capture request replaces the previous owner and raises `PointerCaptureLost` on
that view. The previous press is cancelled, preventing it from remaining pressed or activating on pointer-up. The
normal hit test still runs, so `IsPointerOver` can move to another view while the owner remains pressed.

While capture is active:

- Down, move, and up events are routed only to the capture owner. Ancestors are not added automatically.
- The owner receives pointer-up even if the pointer has moved outside its bounds.
- A pointer leave changes hover state but does not release capture.
- A platform pointer cancel uses the capture/press cleanup path, not normal preview/bubble routing. The capture owner
  receives `PointerCaptureLost`, and the contact's hover and state are removed.

Without capture, `IsPressed` is true only for views shared by the current hover chain and the press chain recorded on
pointer-down. It clears for a view when the pointer leaves that chain and becomes true again if the pointer returns
while the press is still active. The capture owner remains pressed while capture is active, even outside that chain.

Controls that start a drag or similar interaction generally capture during pointer-down and use capture loss to
clean up. This is important because a release can occur outside the original target, and capture can end without a
normal pointer-up reaching that control.

Hover and pressed pseudo-properties are updated as chains and capture state change. This allows styles and controls
to react to pointer state without requiring every control to maintain its own hover bookkeeping.

### Using pointer capture

Pointer capture is for controls that must continue an interaction after the pointer leaves their normal hit area.
Typical examples are dragging a thumb, resizing a window, or implementing a gesture. A control requests capture by
calling its view's `SetCapture` method during its `PointerDown` handler, passing the contact key:

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

Controls that capture should also handle `Panel.PointerCaptureLost`. Capture can end without a normal pointer-up, for
example when a contact is cancelled or another control takes over capture. View removal currently requires the
control's own detach cleanup; automatic router cleanup is future work. Use the handler to clear drag and pressed state,
and make the cleanup safe to run more than once:

```csharp
View.AddEvent(Panel.PointerCaptureLost, OnPointerCaptureLost);

void OnPointerCaptureLost(object? sender, PointerContactKey contactKey)
{
    isDragging = false;
    View.SetProperty(Panel.IsPressed, false);
}
```

When adding or changing a captured interaction, verify these transitions: press inside and release inside, press and
drag outside before release, pointer leave while captured, and capture loss without a normal pointer-up event.

Capture is contact-aware: use `e.Contact.Key` to identify the contact. Setting capture for a key that is not currently
tracked throws; releasing an untracked key is a no-op. `ClearCaptures()` releases all contacts owned by that view.
Capture is not a general-purpose way to make a button clickable. Buttons should normally use the higher-level `Click`
event. Capture keeps low-level pointer events together for an interaction, but it does not by itself make a click valid
when the pointer is released outside the original press target.

### Click-like activation (`PointerClickRecognizer`)

The router does not synthesize or route a generic click event. Controls that need click-like activation use the
public `PointerClickRecognizer` utility, attached to the view that owns the interaction.

The recognizer handles pointer-down and pointer-up events, captures the pointer while the press is active, and
invokes the control's callback only when the pointer is released inside the attached view's bounds. Capture loss,
pointer cancellation cleanup, and release outside the bounds cancel the click.

The recognizer can also receive an optional long-click callback. Its threshold and movement tolerance are configured on
`AppWindow` as `LongClickDuration` (`TimeSpan`) and `LongClickMovementTolerance`; defaults are 500 milliseconds and
8 view-local logical units. The defaults are supplied by the `input.long-click.duration` and
`input.long-click.movement-tolerance` theme variables. The duration theme value uses the standard JSON `TimeSpan` format;
for example, `"00:00:00.5"` represents 500 milliseconds.
The duration starts on pointer-down. Moving farther than the tolerance cancels long-click eligibility, but leaves the
ordinary click behavior unchanged. The callback receives the latest contact in `PointerLongClickEventArgs`. Setting
`Handled` suppresses the ordinary click on release; leaving it false allows the normal click if release is inside.
The duration and tolerance are sampled at pointer-down, so changes apply to the next press.
This flag controls recognizer behavior only and does not retroactively change routed pointer events already dispatched.

A long-click callback can transfer capture to another view with the contact key. The recognizer then receives
`PointerCaptureLost` for that key and ends its own press without releasing the new owner's capture:

```csharp
void OnLongClick(object? sender, PointerLongClickEventArgs e)
{
    textView.View.SetCapture(e.Contact.Key, true);
    e.Handled = true;
}
```

`AppWindow.zui.json5` configures the app-wide defaults through theme tokens. Applications can change the settings at
runtime through `appWindow.Properties.LongClickDuration` and
`appWindow.Properties.LongClickMovementTolerance`.

This allows a `Button`, `Scrim`, `ComboBox`, or third-party control to choose its own higher-level action without
adding another generic routed event to `Panel`. A `Button` uses the recognizer to raise its public `Click` event.
ComboBox item hosts remain panels and attach a recognizer to each item host.

## Current scope

This description focuses on responsibilities and ordering rather than exact event records, view-chain representation,
or rendering data structures. The input path is evolving, so those implementation details are not yet part of the GUI
input contract.

## Remaining issues

The core handled-routing, recognizer-based click, and descendant-capture-cancellation behavior is implemented. The items
below are unfinished work, deliberate behavior differences from Avalonia, or validation still needed before the input
system should be considered complete.

### 6. Handled-events-too and same-view handler stopping

Avalonia supports handlers that explicitly request notification even after an event has been handled. ZurfurGui does not
currently expose an equivalent handled-events-too registration API. This is an API difference, not a defect in ordinary
routing; normal handlers correctly stop at the handled route boundary.

There is a related implementation limitation. Handlers registered on the same view are currently combined into one
delegate. If one handler sets `Handled`, later handlers in that combined delegate still run. The current implementation
can stop routing between views, but not between handlers on one view.

Possible future work is to store ordered handler entries instead of combined delegates, then add an explicit
handled-events-too option. This must preserve handler order and the behavior of `AddEvent` and `RemoveEvent`.

### 8. Capture-key lifecycle

Capture requests identify a tracked contact by its device kind and contact ID. Requests for keys that are no longer
tracked fail when acquiring capture and are ignored when releasing capture. Capture state is GUI-thread state; callers
that trigger capture from a timer or other asynchronous source must marshal the operation to the GUI thread.

### 12. Click and release semantics

ZurfurGui no longer synthesizes a generic routed click event. `Button`, `ComboBox`, `Scrim`, and third-party controls use
the public `PointerClickRecognizer`, which captures on pointer-down and invokes its callback only when pointer-up occurs
inside the attached view's bounds.

This is broadly aligned with Avalonia's control-owned button activation, but the reusable recognizer is a ZurfurGui API
rather than an Avalonia routed event. Release validation uses the attached view's bounds instead of repeating the full
current hit test. That is an intentional simplification and means a release over the view's bounds can click even when
the view's rendered content has more limited hit-test geometry.

Capture loss, pointer cancellation cleanup, and release outside the bounds suppress the recognizer callback. `Handled`
stops routing but does not cancel the control's own press; `Cancelled` stops the current event, while capture loss or
platform cancellation terminates the underlying interaction.

### Automatic cleanup when views are removed

Removing a captured view from the visual tree does not yet automatically cancel its router capture. Controls with their
own detach cleanup can release resources, but the router does not centrally detect every removed captured view.

Future work should release capture, clear press state, and raise `PointerCaptureLost` when a captured view is removed.

### Touch, pen, and multi-contact validation

The WinForms adapter preserves device kind, contact ID, and contact dimensions, but this still needs validation on actual
touch and pen hardware. Testing should cover pointer cancellation, contact promotion to mouse messages, handle recreation,
simultaneous contacts, capture takeover, and view removal while captured.
