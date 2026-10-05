# Input and pointer events

This document describes the current high-level path for mouse and pointer input through ZurfurGui. It starts after
platform or browser input has been received; operating-system input processing is outside the GUI layer.

## Input ingress

Both supported input adapters convert their platform-specific notifications into the same logical pointer events. The
renderer connects that input callback to the pointer router, which processes each event through the four phases below.

The browser adapter registers listeners on the canvas for pointer enter, move, leave, down, and up notifications. It
converts the browser client position into canvas device-pixel coordinates and forwards the event through the shared
`OsCanvas.PointerInput` callback.

The WinForms adapter listens to the canvas control's mouse move, down, up, and leave notifications and forwards the
control coordinates through that same callback. It also observes native Windows pointer messages so touch and pen
contacts retain their device kind and contact ID. Mouse events remain the fallback for ordinary mouse input. The
platform differences end at this boundary; the GUI does not need to know whether an event originated in JavaScript,
native Windows messages, or C# interop.

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
                 capture handling, if active
                                   |
                                   v
              1. Find target and build view chain
                                   |
                                   v
                 2. Preview: root -> target
                                   |
                                   v
                 3. Bubble: target -> root
                                   |
                                   v
                 4. Action and state update
                                   |
                         pointerup may create click
                                   |
                         click repeats phases 1-3
```

Before Phase 1, the router checks for pointer capture and dispatches the event through the captured route when capture is
active. See [Pointer state and capture](#pointer-state-and-capture) for the detailed rules.

## Phase 1: Find the hit target and build the view chain

This phase searches from the app-window root and produces the view chain used by the following routing phases. The
chain contains the target and its ancestors up to the root. If no view is hit, the chain is empty.

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

`IsHit` is therefore called only for a visible view in `Normal` mode, after all children have failed to hit and before
the panel background-alpha fallback. A renderer can define a hit area even when the view has no visible panel
background, as with text. The alpha fallback applies to ordinary panel surfaces and keeps clear panels from becoming
input surfaces merely because they occupy layout space.

The window title drag surface uses `Always` because it is intentionally transparent but must receive events across the
full title-bar area. This changes which view becomes the hit target; it does not change preview or bubble routing.

Overlay controls should keep their full outer view rectangle as the hit target even when their resting visual is only a
thin line or partially transparent. Visual geometry and interaction geometry are separate. This lets a scrollbar or
similar overlay remain easy to target while preserving its compact resting appearance.

**View-chain construction:** this phase builds the current view chain. The chain is consumed by the preview and bubble
phases. It is also used to update hover and pressed state. A pointer-down chain is retained for the later click decision.

## Phase 2: Preview event routing

The preview phase dispatches the current pointer event through the view chain from the root toward the hit target. These
are the `PreviewPointer...` events.

**View-chain use:** this phase consumes the chain built in Phase 1, traversed in root-to-target order. Ancestors receive
the preview event before their descendants.

## Phase 3: Bubble event routing

The bubble phase dispatches the same pointer event from the hit target back toward the root. These are the ordinary
`Pointer...` events.

**View-chain use:** this phase consumes the same current chain built in Phase 1, traversed in target-to-root order.

The current implementation does not model the full WPF routed-event system. In particular, this is a fixed preview-then-
bubble traversal over the views in the chain; the event payload is shared and there is no documented general-purpose
handled/stopping mechanism in this layer.

## Phase 4: Action and state updates

The action phase interprets the routed pointer event and updates interaction state. It is not one additional event route;
it is the event-specific result of processing the previous phases. Pointer-down, pointer-move, and pointer-up each go
through target discovery and preview/bubble routing before their event-specific action occurs.

### Pointer state and capture

The router tracks the current hover target independently from pointer capture:

- `IsPointerOver` follows the result of the current hit test.
- `IsPressed` represents the active press relationship.
- Capture lets a control continue receiving pointer events after the pointer leaves its normal bounds.

Each contact has independent hover, press, and capture state. An uncaptured event routes through its current hit chain.
When capture is active, the captured route receives the event once; it is not also sent to the current hit route. The
normal hit test still runs, so `IsPointerOver` can move to another view while the captured view remains pressed.

While capture is active:

- Move, up, and cancel events are routed to the captured view chain.
- The captured view receives the pointer-up event even if the pointer has moved outside its bounds.
- A pointer leave changes hover state but does not release capture.
- Capture is released after pointer-up or pointer-cancel, and the captured views receive `PointerCaptureLost`.

Controls that start a drag or similar interaction generally capture during pointer-down and use capture loss to clean
up. This is important because a release can occur outside the original target, and capture can end without a normal
pointer-up reaching that control.

Hover and pressed pseudo-properties are updated as chains and capture state change. This allows styles and controls to
react to pointer state without requiring every control to maintain its own hover bookkeeping.

### Using pointer capture

Pointer capture is for controls that must continue an interaction after the pointer leaves their normal hit area. Typical
examples are dragging a thumb, resizing a window, or implementing a gesture. A control requests capture by setting its
view's `CapturePointer` property during its `PointerDown` handler:

```csharp
void OnPointerDown(object? sender, PointerEvent e)
{
    View.CapturePointer = true;
}
```

Release capture explicitly by setting the property to `false` when the interaction ends early:

```csharp
View.CapturePointer = false;
```

Controls that capture should also handle `Panel.PointerCaptureLost`. Capture can end without a normal pointer-up, for
example when a contact is cancelled or the control is removed. Use that handler to clear drag and pressed state, and make
the cleanup safe to run more than once:

```csharp
View.AddEvent(Panel.PointerCaptureLost, OnPointerCaptureLost);

void OnPointerCaptureLost(object? sender, EventArgs e)
{
    isDragging = false;
    View.SetProperty(Panel.IsPressed, false);
}
```

When adding or changing a captured interaction, verify these transitions: press inside and release inside, press and
drag outside before release, pointer leave while captured, and capture loss without a normal pointer-up event.

The `CapturePointer` property is contact-aware. Request capture during the pointer-down event for the contact being
processed; do not use it as a general-purpose way to make a button clickable. Buttons should normally use the higher-level
`Click` event. Capture keeps low-level pointer events together for an interaction, but it does not by itself make a click
valid when the pointer is released outside the original press target.

### Click synthesis

When a pointer-down is processed, its view chain is recorded as the press chain. The pointer-up itself goes through
Phases 1 through 3, producing a new current hit target and view chain. The router then creates a `pointerclick` only
when the contact has an active press and a valid current press chain, and routes that click through preview and bubble
dispatch. A captured pointer-up is delivered through the captured route only, so it is not dispatched twice.

**View-chain use:** the click decision uses the retained press chain and the current chain found during pointer-up. The
synthesized click uses the resulting click chain for preview and bubble dispatch; it does not perform a separate hit
test.

This means a press that moves away before release does not normally activate the original control. Pointer capture can
continue delivering low-level interaction events during the move, but capture does not change the router's separate
hover/press-chain calculation for click eligibility.

Controls usually subscribe to the bubbling pointer-click event and expose a higher-level event. For example, a Button
turns its view's pointer click into its public `Click` event. Other controls can subscribe directly to the low-level
pointer events when they need movement, capture, or preview behavior.

## Current scope

This description intentionally focuses on responsibilities and ordering rather than the exact event record, view-chain
representation, or rendering data structures. The input path is evolving, so those implementation details should not be
considered part of the GUI input contract yet.

## Recommended architectural improvements

These are follow-up recommendations for the current input system. Keep this section updated as each decision is made.

### 1. Add descendant capture cancellation

Capture ownership is already tracked centrally by the pointer router for each contact. The remaining requirement is a
subtree operation that allows a container to cancel capture held by any descendant.

The future API should support operations equivalent to:

```text
CapturePointer(view, contact ID)
ReleasePointerCapture(view, contact ID)
CancelPointerCaptureInSubtree(container, contact ID)
```

The subtree operation allows a container such as `ScrollViewer` to cancel capture held by a child such as a button
without knowing which exact child owns it. Cancellation must always raise the existing capture-lost notification so
the child can clear drag and pressed state.

### 2. Add event cancellation deliberately

The current preview and bubble phases do not provide a general handled or cancelled result. Before adding
special-case events, consider adding a small event-routing state that can express whether an event was handled or
cancelled.

This would allow a container to prevent a child action or cancel a gesture without inventing a separate event for every
case. The behavior must be defined carefully: handled should suppress an appropriate higher-level action, while cancel
should terminate an interaction such as capture or click recognition. Do not add this until the capture ownership rules
are explicit enough to define what cancellation means.

### 3. Validate touch and pen input in WinForms

The WinForms adapter now supplements ordinary mouse events with a native `WM_POINTER` hook. It identifies touch and
pen contacts, preserves their pointer IDs, and emits `OsPointerEvent` values while retaining mouse events as the
fallback.

Remaining work is validation on actual touch and pen hardware, including contact dimensions, pointer cancellation,
mouse-message promotion, handle recreation, and simultaneous contacts. Any platform-specific fixes should remain in
the WinForms adapter and continue to emit the shared `OsPointerEvent` boundary type.

### Suggested implementation order

Implement the changes in this order:

1. Add ancestor or subtree capture cancellation.
2. Validate touch and pen input on supported WinForms hardware.
3. Add handled or cancelled event behavior if actual controls require it.

## Current handoff status

The pointer-contact model is implemented. The raw platform boundary and normalized framework event are separate, hit
testing receives a `HitTestContext`, and router state is keyed by contact identity. The remaining recommendations below
are follow-up architecture work rather than unfinished pointer-contact modeling:

- `src/ZurfurGui/Platform/OsPointerEvent.cs` contains the raw string event and platform metadata.
- `src/ZurfurGui/Input/Events.cs` contains `PointerEventKind`, `PointerContact`, and normalized `PointerEvent`.
- `src/ZurfurGui/Platform/OsCanvas.cs` exposes the raw event through the public platform callback.
- `src/ZurfurGui/Input/PointerOver.cs` converts the raw event before hit testing and routing.
- `src/ZurfurGui.Browser/Platform/BrowserCanvas.cs` supplies browser pointer metadata.
- `src/ZurfurGui.WinForms/Platform/WinCanvas.cs` supplies mouse fallback metadata and native touch/pen pointer events.
- `src/ZurfurGui/Input/HitTestContext.cs` carries the `PointerContact` into hit testing.
- `src/ZurfurGui/Input/HitTestMode.cs` defines the view hit-test modes.
- `src/ZurfurGui/Render/Renderable.cs` and `src/ZurfurGui/Render/RenderHelper.cs` accept the context.
- `src/ZurfurGui/Input/PointerOver.cs` creates and propagates the context during target discovery.

The full solution builds successfully. The next practical step is manual WinForms testing of mouse, touch, pen, and
multi-contact capture behavior, followed by a dedicated test form when that input path is confirmed.

Known limitations to address later:

- Native WinForms pointer handling still needs runtime verification on touch and pen hardware if touch and pen support
  is included in the beta scope.
- Pointer capture and click cleanup still need broader multi-contact runtime testing.
- Descendant capture cancellation and general event cancellation are not implemented.
