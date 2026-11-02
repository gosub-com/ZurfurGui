# Input Remaining issues

**NOTE:** This document is in a state of flux.  Some issues here have already been fixed.

The core handled-routing, recognizer-based click, and descendant-capture-cancellation behavior is
implemented. The items below are unfinished work, deliberate behavior differences from Avalonia, or
validation still needed before the input system should be considered complete.

### 6. Handled-events-too and same-view handler stopping

Avalonia supports handlers that explicitly request notification even after an event has been handled.
ZurfurGui does not currently expose an equivalent handled-events-too registration API. This is an API
difference, not a defect in ordinary routing; normal handlers correctly stop at the handled route
boundary.

There is a related implementation limitation. Handlers registered on the same view are currently combined into one
delegate. If one handler sets `Handled`, later handlers in that combined delegate still run. The current implementation
can stop routing between views, but not between handlers on one view.

Possible future work is to store ordered handler entries instead of combined delegates, then add an
explicit handled-events-too option. This must preserve handler order and the behavior of `AddEvent` and
`RemoveEvent`.

### 8. Capture-key lifecycle

Capture requests identify a tracked contact by its device kind and contact ID. Requests for keys that are
no longer tracked fail when acquiring capture and are ignored when releasing capture. Capture state is
GUI-thread state; callers that trigger capture from a timer or other asynchronous source must marshal the
operation to the GUI thread.

### 12. Click and release semantics

ZurfurGui no longer synthesizes a generic routed click event. `Button`, `ComboBox`, `Scrim`, and
third-party controls use the public `ClickRecognizer`, which captures on pointer-down and invokes its
callback only when pointer-up occurs inside the attached view's bounds.

This is broadly aligned with Avalonia's control-owned button activation, but the reusable recognizer is
a ZurfurGui API rather than an Avalonia routed event. Release validation uses the attached view's bounds
instead of repeating the full current hit test. That is an intentional simplification and means a
release over the view's bounds can click even when the view's rendered content has more limited hit-test
geometry.

Capture loss, pointer cancellation cleanup, and release outside the bounds suppress the recognizer
callback. `Handled` stops routing but does not cancel the control's own press; `Cancelled` stops the
current event, while capture loss or platform cancellation terminates the underlying interaction.

### Automatic cleanup when views are removed

Removing a captured view from the visual tree does not yet automatically cancel its router capture.
Controls with their own detach cleanup can release resources, but the router does not centrally detect
every removed captured view.

Future work should release capture, clear press state, and raise `PointerCaptureLost` when a captured view is removed.

### Touch, pen, and multi-contact validation

The WinForms adapter preserves device kind, contact ID, and contact dimensions, but this still needs
validation on actual touch and pen hardware. Testing should cover pointer cancellation, contact promotion
to mouse messages, handle recreation, simultaneous contacts, capture takeover, and view removal while
captured.



# Gesture recognizer cleanup plan

This document lists the open bugs, design gaps, and simplifications in the gesture layer (`GestureArena`,
`IGestureRecognizer`, `GestureSessionContext`, `ClickRecognizer`, and the gesture hooks in
`PointerRouter`). Completed work is not listed. It covers: accept or reject during `CreateSession`,
exception safety in session callbacks, the `Candidate` object, the single end path in the arena, and
removal of `GestureRecognizerLifecycle` (recognizers are now attached with
`View.AddGestureRecognizer` and detached with `View.RemoveGestureRecognizer`; `IGestureRecognizer` is
not `IDisposable`).

Each item is tagged with its type:

- **Bug fix**: current behavior is wrong or unsafe.
- **Design gap**: not a bug today, but it blocks pan/zoom or a stable public API.
- **Cleanup**: behavior stays the same; the code gets smaller or clearer.

Items marked **necessary before pan/zoom** must be finished before the pan/zoom recognizer is started.

## Suggested order

1. Items 1 through 4 (bug fixes in capture, routing, and detached views).
2. Items 5 through 9 (design gaps for pan/zoom).
3. Items 10 through 12 (docs, smaller cleanups, manual test coverage).

Run the click and capture checks in the TestApp after every item. There is no automated test project.

## Bug fixes

### 1. Raw handlers are not told when a gesture steals the contact (bug fix)

Status: ***fixed***

**necessary before pan/zoom**: a button inside a pannable area must lose its pressed state when the pan wins.

`CaptureFromGesture` clears the contact's press chain and captures the winning view, but views that were
pressed receive no notification. A child using raw `PointerDown` and `PointerUp` never learns its press
was taken away. Only the previous capture owner receives `PointerCaptureLost`.

How to fix:

- In `PointerRouter.CaptureFromGesture`, raise `PointerCaptureLost` on each view returned by
  `state.CancelPress()` that is not the new owner.
- Document the arbitration policy for raw `SetCapture` versus a pending gesture ("raw capture wins") in
  `gesture-recognizer.md` and in the `View.SetCapture` XML comments.

### 2. Down routing after a gesture accepts on pointer-down (bug fix)

Status: ***fixed***

If a recognizer accepts during `Start`, capture is taken, but `wasCaptured` in
`PointerRouter.PointerInput` was computed earlier, so the raw down still routes through the hit chain
instead of the capture owner.

How to fix: re-read `state.CapturedView` after `GestureArena.Start` and route the raw down to the
capture owner. The alternative is to document the current behavior; the code fix is preferred because
it matches the documented "the capture owner receives down, move, and up" model.

### 3. Contact state cleanup after pointer-up for touch and pen (bug fix, verify first)

Status: ***fixed*** `PointerRouter` now exits the hover chain and removes the contact state on `Up` for
touch and pen, so it no longer depends on the platform sending a trailing `Leave`. A later `Leave` is
harmless. Mouse keeps its hover state after `Up`. WinForms touch and pen are not yet tested.

On `Up`, the router clears press state but does not call `RemoveContactState`. For a mouse this is
intended because hover persists. For touch or pen, a missing trailing `Leave` leaves a stale entry in
`_contactStates` and stale hover chain membership. With multi-contact gestures (item 5) stale entries
could be mistaken for live contacts.

How to fix: verify that the WinForms and browser adapters always send `Leave` or `Cancel` after a
touch or pen `Up`. If they do not, remove the contact state on `Up` for non-mouse devices, after the
hover notifications.

### 4. Views removed or hidden mid-gesture (bug fix)

Status: ***fixed*** Removal: `View.RemoveChild` calls `PointerRouter.CancelViewTree` while the child is
still attached. It cancels gesture candidates, releases capture (with `PointerCaptureLost`), and drops
the view and its descendants from the hover and press chains. Reparenting is a remove followed by an
add, so it also cancels. Hiding: when a view's `IsVisible` becomes false, `View` calls the same method,
which covers its descendants (and therefore hidden ancestors) through a parent-chain test. A view
hidden before the arena starts cannot become a candidate, because hit testing skips invisible views and
their subtrees.

**necessary before pan/zoom**: a pan target removed during a drag or inertia must not keep running.

The arena holds candidate views and never checks that they are still in the tree. A removed view can
still receive gesture input and capture, and the click recognizer's `PreRenderFrame` subscription stays
alive until the arena ends. Controls do not remove recognizers in `OnDetach` because `OnDetach` also
runs when a view is reparented.

How to fix:

- When a view is detached from the app window (not merely moved), ask the router to cancel any arena
  whose candidate or capture owner is the view or a descendant.
- At minimum, check `candidate.View.AppWindow is null` when dispatching and cancel that candidate.
- Inertia animations (item 7) must also stop when the target is detached.

## Design gaps for pan/zoom

### 5. The arena is single-contact (design gap, major)

**necessary before pan/zoom**

Each contact gets its own arena, sessions are created per contact, and capture is per contact. A pinch
needs two contacts to reach one session, and the winner needs both contacts captured.

How to do it:

- Add an optional session interface, for example `IMultiContactGestureSession`, with
  `OnContactAdded(GestureSessionContext context)` and `OnContactRemoved(PointerContact contact)`.
- When a new contact goes down, the router first looks for an existing arena (accepted or pending)
  whose candidate view is in the new contact's hit chain. If its session implements the interface,
  offer the contact to it rather than starting an independent arena.
- Winner capture then runs per contact: call `CaptureFromGesture` for each contact the winner owns.
- When one contact goes up and others remain, the session keeps running. The arena ends only when the
  last contact is up or canceled.
- Decide what happens when a second contact lands on a view whose arena already has a winner on a different view.
- Keep single-contact recognizers (click) unchanged: they simply do not implement the new interface.

Note that `CancelRecognizer`, `ClearCaptures`, and `ReleaseGestureCapture` currently assume one arena per contact
state; they must be revisited together with this item.

### 6. No auto-win and no configurable slop (design gap)

Status: ***done for pan*** Slop is now `AppWindow.Properties.MovementSlopMouse` and `MovementSlopTouch`
(theme `input.slop.mouse` and `input.slop.touch`), shared by long click and `PanRecognizer`. Remaining:

- Slop is in device pixels and is not scaled by DPI. Scale it when DPI is available.
- Pen uses the mouse slop. Revisit if pen needs its own value.
- Click does not reject a touch that moved past the slop when no pan wins (for example a drag along an
  axis the viewer cannot scroll). Consider rejecting past the slop for touch.
- Children inside a scrolling viewer show their pressed state immediately. Consider a short delay so a
  button does not flash when a pan starts.
- Axis locking (a mostly vertical drag in a two-axis viewer locks to vertical) and overscroll are not
  implemented.

**original description**

**necessary before pan/zoom**

A last remaining candidate does not win when the others reject. That is fine for click, but pan/zoom
needs an explicit rule and a slop distance.

- Keep the rule "a recognizer must accept explicitly" and document it.
- Add a pan slop property next to `LongClickMovementTolerance` in the app window properties, and use it
  from the pan recognizer. Do not hard-code it. Consider separate values for touch and mouse.

### 7. Pan, flick, and zoom need timestamps and a shared clock (design gap)

Status: ***partly done*** `PointerEvent.Timestamp` is supplied by the browser and WinForms touch/pen
adapters, with a router stamp otherwise. `ClickSession` long-click movement now uses device positions.
The click release test still uses `OriginRect`. `PreRenderFrame` still passes no elapsed time (the
ScrollViewer flick keeps its own `Stopwatch`).

**original description**

**necessary before pan/zoom**

`PointerContact` and `PointerEvent` carry no timestamp, and `PreRenderFrame` passes no elapsed time.

- Either add a timestamp to `PointerEvent` populated by the platform adapters, or add one shared clock
  on the app window used by every recognizer. Do not let each recognizer create its own `Stopwatch`.
- Use device positions (`Contact.DevicePosition`) for pan/zoom math.
- Change `ClickSession` to compare device positions rather than view-relative positions, so content
  moving under the pointer (a scrolling parent) does not make a click drift. The click currently tests
  release against `CandidateView.OriginRect`; verify this is in device space and stays correct while the
  view moves.

### 8. No wheel or trackpad-zoom event (design gap)

Status: ***partly done*** A separate wheel event and router path now exist. The remaining work is listed
under [Mouse Wheel Improvements](#mouse-wheel-improvements).

Desktop zoom usually comes from the wheel or a trackpad rather than two fingers. Add a wheel event (or a
separate wheel callback) to `OsPointerEvent5`, the router, and the browser and WinForms adapters. Decide
whether it routes through the preview and bubble phases and how a recognizer opts in. Keep it out of the
arena, because a wheel has no down/up lifetime. Not required for touch pan, but required for desktop
zoom.

### 9. Mouse-button policy is not visible to recognizers (design gap)

**necessary before pan/zoom**

The session context exposes the initial `Contact`, including `Buttons` and `Modifiers`, but the click
recognizer accepts any button. Decide which buttons the click recognizer responds to (currently all),
and make sure a pan recognizer can filter by device, button, and modifier without the arena creating
sessions it will always reject. Consider an optional `IGestureRecognizer.CanStart(PointerContact)`
check before `CreateSession`.

## Cleanups and documentation

### 10. Stop copying `PointerEvent` per session (cleanup)

The arena builds a new `PointerEvent` for each session and event, and the docs explain that `Handled` is
independent. Share one copy per dispatch (so gesture events cannot affect raw routing) and shorten the doc.

### 11. Minor cleanups (cleanup)

- `ClickRecognizer.ValidateArguments`: replace with `ArgumentNullException.ThrowIfNull` in the constructor.
- Long-click timer: move the `Stopwatch` and `PreRenderFrame` logic into a small internal helper that
  pan and flick can reuse, behind the shared clock from item 7.
- `GestureArena` candidate creation: avoid `ToArray()` of the recognizer list for every view on every pointer-down.
- `PointerRouter.PointerInput`: remove the double `ToList()` in `UpdateViewChain(previousHoverChain.ToList(), ...)`.
- Move-path allocations (`GetViewChain`, `SendPointerEvent`'s `views.ToList()`, hover and press
  snapshots): leave for a later performance pass, but do not add new per-move allocations in the
  pan/zoom recognizer.
- `PointerRouter.ClearPointerCapture` writes `Debug.WriteLine("CaptureLost")`; remove it.
- Fix the `PointerEvent` XML comment typo `View.ToClinet`.

### 12. Thread affinity and manual test coverage (doc only)

**necessary before pan/zoom**: pan/zoom adds timers and frame callbacks, so these rules must be written
down first.

- State that gesture APIs, including `GestureSessionContext.Accept` and `Reject`, are UI-thread only.
  The arena is not thread-safe. Update `gesture-recognizer.md` and the XML comments on
  `GestureSessionContext`.
- There is no test project. Before pan/zoom, extend the TestApp input surface with a scrollable area
  containing a button and selectable text so click-versus-pan, capture loss, and detach-mid-gesture can
  be checked by hand. Consider a test project with a fake clock and fake router input once the clock
  exists.

### Mouse Wheel Improvements

The first wheel implementation (see `input-events.md`, "Wheel events", and `scroll-viewer.md`,
"Mouse wheel") is deliberately small. `PointerRouter.WheelInput` hit-tests, walks from the hit view
toward the root, and gives the `WheelEvent` to the first view that has a `Panel.PointerWheel` handler.
That view consumes the wheel, and there is no preview phase, no `Handled` flag, and no capture
involvement. The items below are known limitations, listed with how to implement each one and how hard
it is. No code has been changed for them yet. Wheel input stays out of the gesture arena, because a
wheel has no down/up lifetime.

Suggested order: W1 and W2 first (they change handler semantics together), then W3, W4, and W5, with
W6 through W8 as time allows.

#### W1. Handlers cannot decline the wheel (design gap)

Difficulty: **trivial**

Today the first view with a handler consumes the wheel, even a ScrollViewer that is already at the end
of its range, so an inner scroller never passes the wheel to an outer one. Most frameworks (WPF,
Avalonia, the DOM) let a handler decline so a parent can take the event.

How to implement:

- Add `public bool Handled { get; set; }` to `WheelEvent`. A record can have a settable property; it
  does not need inheritance.
- In `PointerRouter.WheelInput`, keep walking toward the root after a handler returns, and stop only when
  `ev.Handled` is true. Views with no handler are still skipped.
- In `ScrollViewer.OnPointerWheel`, set `Handled = true` only when the offset actually changed (compare
  the clamped `ScrollOffset` before and after). This gives chained scrolling at the end of the range.
- Decide the policy and then document it. If the current "never chains" behavior is still wanted, add a
  styled property on ScrollViewer (for example `wheelChaining`, default to the chosen behavior) instead
  of hard-coding it.
- Update the "Wheel events" section of `input-events.md` and the "Mouse wheel" section of
  `scroll-viewer.md`, which both say the wheel never reaches a parent handler.

Any new handler must set `Handled` when it consumes the wheel, otherwise the walk continues to its ancestors.

#### W2. No preview phase for ancestors (design gap)

Difficulty: **low** (after W1)

A parent such as a zoomable canvas cannot take the wheel before a child ScrollViewer sees it. The pointer events have
a preview phase for this; the wheel does not.

How to implement:

- Add `$previewPointerWheel` (`EventHandler<WheelEvent>`, `attachedProperty`) to `Panel.zui`, next to
  `$pointerWheel`.
- In `PointerRouter.WheelInput`, build the full chain once. Run preview from the root toward the hit
  view, stopping when `Handled` is set, then run the bubble walk from W1 from the hit view toward the
  root.
- Keep both routes allocation-light: the chain list is the only allocation, as it is today.
- Document that preview handlers see the wheel even when the pointer is over a child that would handle
  it, and that handling it in preview suppresses the bubble route (same rule as the pointer events).

#### W3. Wheel ignores pointer capture (design gap)

Difficulty: **low to medium**, needs a policy decision first

The wheel always goes to the view under the pointer. That matches browsers and Win32, and is right for
most controls. Some interactions want the wheel to keep going to the captured control, for example
scrolling a list while drag-selecting in it.

How to implement:

- Decide the rule. The least surprising one is: if the mouse contact is captured, offer the wheel to the
  capture owner first (as a normal route with that single view), and fall back to the hit chain if it
  does not handle it.
- `OsWheelEvent` uses `ContactId` 0 and `Device` mouse, so look up the contact state the same way mouse
  pointer events do. Add a read-only lookup on the router for the capture owner of a contact key; do not
  create contact state for a wheel that arrives before any pointer event.
- This depends on W1, because the fall back needs to know whether the capture owner handled the event.
- Document the rule in `input-events.md` and in the `View.SetCapture` XML comments.

#### W4. Wheel hit testing over transparent areas (verify first, possible bug)

Difficulty: **trivial to low**

The wheel uses the same hit test as pointer events. A view with no control renderer and a background alpha of 16 or
less is not hit, so a wheel over an empty, transparent part of a ScrollViewer's content may find no target and do
nothing, even though that point is inside the viewer. This is unverified; check it in the TestApp first.

How to implement (if it reproduces), pick one:

- Give the ScrollViewer's viewport view `HitTest = Always` in `ScrollViewer.zui` or its constructor. Children
  are still searched first, so only otherwise-empty viewport area is selected. Side effect: that area becomes a
  pointer and hover target.
- Or add a wheel-specific hit test mode that ignores the background alpha fallback and treats any visible view whose
  bounds contain the point as a target. This leaves pointer behavior unchanged but adds a second hit-test path to
  maintain.

#### W5. Platform delta normalization is approximate and untested (design gap, verify)

Difficulty: **low**, mostly manual testing

The wheel unit is the browser pixel-mode delta, 100 per mouse detent. That is the common value for Chromium on
Windows, but it is not guaranteed:

- Chromium on other platforms, Firefox (line mode), and Safari report different magnitudes, and the value can change
  with browser zoom or system scroll settings.
- Trackpads report many small deltas, often with momentum after the fingers lift. Momentum is passed through as
  ordinary wheel events, so the viewer will keep scrolling for a moment.
- WinForms converts from 120 units per detent with a fixed scale. High-resolution wheels and precision touchpads
  report deltas smaller than 120; they pass through proportionally but have not been tried. Horizontal wheel comes
  from `WM_MOUSEHWHEEL` handled in the native window and is also untested.
- Page-mode deltas in the browser are scaled by the canvas height, which has not been verified.

How to implement:

- Test on Chrome, Edge, Firefox, and a Windows precision touchpad, and record the measured delta per detent.
- If the values differ too much, normalize in the adapters (not in the router or ScrollViewer) and keep
  `WheelEvent.UnitsPerNotch` as the single definition of one detent.
- Consider honoring the system "lines per wheel notch" setting in WinForms
  (`SystemInformation.MouseWheelScrollLines`) so a Windows user's setting is respected. Note that `wheelSteps` from
  the theme is currently the only step control.

#### W6. Ctrl+wheel is not available to applications (design gap)

Difficulty: **medium**

Both adapters drop Ctrl+wheel because the browser and the WinForms form use it to magnify the whole window. An
application therefore cannot implement its own zoom (for example zooming a canvas or text editor) with Ctrl+wheel.

How to implement:

- Decide who owns the gesture. Either keep it reserved for window magnification, or let the GUI see it first and
  forward it to the platform when nothing handles it.
- Forwarding when unhandled needs W1 (`Handled`) and an asynchronous problem in the browser: `preventDefault` must be
  called synchronously in the JavaScript event handler, before the C# router runs. The adapter would have to ask the
  router synchronously (a "would anything handle this?" query) or always prevent the default and apply the browser
  zoom itself, which is not possible.
- In WinForms the form's `MouseWheel` handler and the canvas both see the event, so it can be done by letting the
  canvas report `Handled` back to the form.
- Because of the browser limitation, a simpler option is a per-application switch that disables window magnification
  and forwards Ctrl+wheel to the GUI.

#### W7. ScrollViewer wheel behavior is basic (design gap, optional)

Difficulty: **low** each

- Scrolling jumps by the full distance in one frame. Smooth scrolling would animate toward a target offset using the
  shared clock from item 7, and must stop when the view is detached (item 4).
- Only `Shift` swaps the axis. Consider honoring the platform's natural-scrolling setting if the browser reports it.
- `wheelSteps` of zero or a negative value is accepted and is not validated. Decide whether to clamp it in the
  handler or reject it when the theme is loaded.
- The scrollbar arrows use `smallChange` but the wheel uses `wheelSteps * scrollSize`. This is intentional, but the
  two controls should be kept consistent if either changes.
- Other scrolling controls (ListBox, ComboBox popup, text editing) do not handle the wheel yet. Each should share the
  `scroll.wheel.steps` theme variable rather than defining a new one.

#### W8. No manual test coverage for the wheel (doc only)

Difficulty: **low**

There is no test project, and no wheel sample in the TestApp. Add a ScrollViewer sample (or extend the input surface
from item 12) that covers:

- A single ScrollViewer: vertical wheel, Shift+wheel, horizontal wheel, wheel over the content, over empty space (W4),
  and over its scrollbars.
- Two nested ScrollViewers, to confirm the current "no chaining" behavior and, after W1, the chained behavior.
- `scrollMode` set to Horizontal and Vertical only.
- Wheel while dragging a scrollbar thumb (W3).
- Ctrl+wheel still magnifies in both the browser and WinForms.
- A raw `PointerWheel` handler on a plain panel that logs the event, so the deltas can be read on each platform (W5).

Consider a test project with a fake canvas that raises `OsWheelEvent` once the manual coverage in item 12 exists;
router wheel routing and `ScrollViewer.ScrollOffset` clamping are easy to test that way.



## TBD in the future

The next feature is drag panning with optional inertial flicking, initially for `ScrollViewer` and ultimately reusable
on any view. The design discussion and agreed direction are recorded here.

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


# Public API cleanup recommendations for 10/31/2026

This section lists public API risks to resolve before a 1.0 release, to reduce breaking changes
after release. It is based on a source survey; verify each type's current accessibility and usage
before changing it. Making a type internal now can be reversed later without breaking consumers;
removing a released public type is a breaking change.

Ratings:

- **Difficulty**: trivial, easy, medium, hard, or very hard.
- **Importance**: critical, high, medium, or low.

### A1. Platform host contracts are public and unversioned

Status: ***Done (visibility)***: `OsCanvas`, `OsContext`, and `OsWindow` are now internal and
the host projects see them through `InternalsVisibleTo`. Reshaping the members (callbacks,
setters, `MarshalString`) is still open.

Difficulty: hard. Importance: critical.

`OsCanvas`, `OsContext`, and `OsWindow` in `Platform/` are implemented by host projects. Their
contracts include single-subscriber `PointerInput` and `WheelInput` callbacks, mutable size
properties, a default `DevicePixelSize` implementation, and `OsContext.MarshalString`, a low-level
interop detail. Fonts are identified by string names.

Fix: decide which members hosts need. Replace single-subscriber callbacks with events or an explicit
registration method; restrict setters that hosts should not control; remove interop helpers from the
public host contract; and document `Platform` as a separately versioned host API. Provide defaults
for future interface members where appropriate.

### A2. OsPointerEvent and OsWheelEvent are public wire formats

Status: ***Done (visibility)***: both types are now internal and sealed. The string `Type` is kept
because it matches the platform event names. Constants for the names are still open.

Difficulty: medium. Importance: critical.

These positional records include a string `Type`. Adding a field breaks constructor call sites, and
the string value can be misspelled.

Fix: replace `Type` with a defined event kind, and use sealed types with init-only properties instead
of positional records. Update the platform adapters and avoid making deconstruction part of the
contract.

### A3. OsRenderBuffer and OsRenderCommand expose the render encoding

Status: ***Done***: both types are internal and visible only to the host projects.

Difficulty: easy. Importance: high.

These public types expose low-level render command plumbing. `OsRenderBuffer` also has a note about
removing heap allocation, suggesting its representation may change.

Fix: make both types internal if host projects do not need them. If a host needs rendering access,
expose only the stable operations it requires through a narrow contract.

### A4. Generated-code contract is public (`Controllable`, `Loader`)

Status: ***Partly done***: the generator hooks are split from the application-facing API.
`Controllable` now has only `View`, `ContentHost`, `SetDataProperty`, `OnAttach`, and `OnDetach`.
The generated-only members moved to `ZurfurGui.Generated.ZuiControllable`, and the generated calls
go through `ZurfurGui.Generated.ZuiRuntime`; both are hidden from IntelliSense. `Loader` keeps only
`JsonSerializerOptions` and `RegisterJsonTypeInfoResolver` public. Template and parent content loading is now done by the
loader itself, so `LoadTemplateContent`, `LoadParentContent`, and the public `ControlCreationContext`
are gone. `ZurfurJsonContext` is now internal; applications use `Loader.JsonSerializerOptions`, which
adds the custom converters, and `RegisterJsonTypeInfoResolver` for their own types. Still open: add a
minimum-version check (below).

Difficulty: hard. Importance: critical.

Generated code is compiled into the application, so it cannot use `InternalsVisibleTo`. The
generated-only members (`ZuiControllable`, `ZuiRuntime`) are therefore still public, but isolated
in `ZurfurGui.Generated` and hidden from IntelliSense.

Applications get the generator and library together from one package, so they cannot drift apart.
The risk is a third-party control library compiled against a newer ZurfurGui than the application
runs. Its generated code then fails with a `MissingMethodException` in the middle of a load.

Remaining fix, before 1.0:

- Make the generated-code surface additive-only after 1.0 (see the compatibility table in
  `code-generation.md`). New `ZuiControllable` members need default implementations.
- Add a minimum-version check. `ZuiRuntime.Version` is bumped when generated-code surface is added.
  The generator emits an assembly attribute with the lowest version the generated code needs, and
  `ZuiRuntime` checks once per assembly that it is not greater than `ZuiRuntime.Version`, with a
  clear "upgrade ZurfurGui" error. Older generated code on a newer library is allowed.

### A5. `.zui` directives and property names are a language contract

Difficulty: medium. Importance: high.

Directives and property names such as `$offset`, `$visualOffset`, `$hitTest`, and
`keyName: "Panel.Offset"` are consumed by `.zui` files. Renaming them can break every application
using those files.

Fix: document the supported directives and built-in property names, review them for consistency,
and treat them as compatibility-sensitive after 1.0. Add an explicit migration/deprecation path for
any future rename.

### A6. `Controllable.OnDetach` is public and now documented

Status: ***fixed***. `OnDetach` was already invoked; only the source comment claimed otherwise.

Difficulty: easy. Importance: high.

`View.RemoveChild` (and therefore `RemoveFromParent` and `ClearChildren`) calls `OnDetach` on the
removed view and its descendants, children first, while they are still attached to the `AppWindow`.
It does not run for views that were never attached to an `AppWindow`. The `Controllable` comment now
documents exactly when it runs.

### A7. Gesture interfaces may need new members

Difficulty: medium. Importance: high.

`IGestureRecognizer` and `IGestureRecognizerSession` are interfaces implemented by consumers.
Multi-contact gestures, priority, wheel, or keyboard support may require new callbacks, which would
break existing implementations. `Layoutable`, `Renderable`, and `Controllable` have the same
interface evolution risk.

Fix: settle expected gesture input and multi-contact needs before release. Prefer sealed context or
options objects for extensible data, or default interface implementations when supported by all
target frameworks. Avoid adding required interface members in a compatible release.

### A8. `PointerContact` is a positional record

Difficulty: medium. Importance: high.

`PointerContact` has eight positional parameters and may need more input data, such as pressure or
tilt. Constructor calls and deconstruction are part of the public API.

Fix: replace positional construction with a stable type using named init-only properties, and avoid
public deconstruction. Update all platform adapters and callers. Review `WheelEvent`,
`PointerContactKey`, and `CapturedPointerContact` for the same positional-shape risk.

### A9. Pointer event records expose inheritance and value semantics

Difficulty: easy. Importance: medium.

`PointerEvent` and `WheelEvent` expose record behavior, including equality, `with`, and deconstruction;
their mutable flags are part of the API. `PointerEvent.Timestamp` also needs a stable definition of
whether the platform or router supplies it.

Fix: seal the event types or use sealed classes, retain only the mutation required for routing, and
document timestamp source and units. Do this before consumers depend on record equality or
subclassing.

### A10. `CapturedPointerContact` may expose the view tree

Difficulty: easy. Importance: medium.

The public type contains a `View` reference, exposing an internal tree relationship through a
contact-state API.

Fix: check all consumers. Make the type internal if only router implementation code needs it;
otherwise expose only the stable contact key and the minimum necessary public information.

### A11. Public enums will likely grow

Difficulty: trivial. Importance: medium.

`PointerDeviceKind`, `PointerEventKind`, `DockEnum`, `HitTesting`, `PanAxes`, and `RenderResult` may
gain values. `PointerButtons` and `PointerModifiers` are byte-backed flags with at most eight bits.
`ViewFlags` is also explicitly sized.

Fix: decide whether the backing types leave room for expected values. Document that consumers should
handle unknown enum values with a default case. Widen flag enums before 1.0 if more bits are
plausible, and audit serialization or interop uses before changing their underlying types.

### A12. Mutable public structs

Difficulty: easy. Importance: high.

`Vector`, `Size`, and `Thickness` have settable properties on structs. Mutating a copy may not mutate
the original, and changing the members to immutable after release is source-breaking.

Fix: make these readonly value types with constructor-only or init-only properties, then update
callers that mutate fields. Check `Color` and `Point` for the same issue before deciding their
public shape.

### A13. Pan options expose unstable tuning and units

Difficulty: medium. Importance: high.

`PanOptions` exposes mutable settings including `Slop`, `MinFlickVelocity`, and `MaxFlickVelocity`.
Their units are device pixels, and source comments say slop is not DPI-scaled. Changing units later
would silently alter gesture behavior.

Fix: choose and document device or logical units before 1.0. If logical units are intended, apply DPI
conversion consistently and test on each platform. Keep future settings in an options type rather
than extending recognizer constructors positionally.

### A14. Control event signatures may be hard to extend

Difficulty: easy. Importance: medium.

`Button.Click` uses `EventHandler?`, while `Scrim.Dismissed` uses `EventHandler<PointerEvent>`. The
event signatures cannot carry additional event data later without a breaking change.

Fix: decide whether consumers need click or dismissal details. If so, define dedicated event-args
types now and use them consistently. Otherwise document the intentionally minimal event contract.

### A15. Public implementation and helper types

Difficulty: easy. Importance: high.

The API review identified `Renderer`, `RenderContext`, `MeasureContext`, stats types, `ObjectCache`,
`LruDictionary`, `TextLinesBuilder`, `TextLines.Enumerator`, `Hasher`, and some `ThemeManager`
helpers as possible implementation details. Public non-sealed classes also allow consumers to
subclass types that may not be designed for extension.

Fix: verify usage by application and host code. Make implementation-only types internal, seal
remaining public classes that are not extension points, and document intended inheritance contracts
for types that remain extensible. Check whether `TextLines` should be sealed. Keep `ThemeManager`
helpers public only if consumers need them and their parameter types are stable.

### A16. Unfinished or duplicate public API

Difficulty: easy. Importance: high.

The API review found `LayoutColumn` described as replaceable by `LayoutRow`, `LayoutRow` with a TBD
about supporting columns, `Window.IsWindowWrappingVisible` described as needing a real property,
`AppWindow` settings described as temporary, and `View` component lookup described as needing
substantial improvement. `AppWindow` also appears to include a test-only `DataTest` binding.

Fix: decide the final names and semantics before release. Remove unfinished public members or mark
them obsolete with a migration path. Move test-only bindings into TestApp and verify they are not in
the shipped control.

### A17. Public features and data contracts with known gaps

Difficulty: medium. Importance: medium.

The review noted clipping documented as disabled, generated properties with unimplemented data
binding, and a TBD about `Color` channel order on the browser canvas.

Fix: implement each advertised feature or remove it from the public surface until ready. Confirm and
document color channel order across platforms. Do not promise behavior that is currently disabled
or inconsistent.

### A18. Track the public API automatically

Difficulty: easy. Importance: high.

Without an API baseline, accidental public changes can pass unnoticed in review.

Fix: add `Microsoft.CodeAnalysis.PublicApiAnalyzers` to the library project, generate shipped and
unshipped API baselines, and require baseline changes to be reviewed alongside API changes. First
verify target-framework and analyzer compatibility with both .NET 10 and .NET Standard 2.0.

### Suggested order

1. Establish the exact API inventory and baseline (A18), then hide confirmed implementation details
   (A3, A10, A15).
2. Resolve small, source-breaking type decisions before release (A9, A11, A12, A14).
3. Settle extensible input and record shapes (A2, A7, A8, A13).
4. Stabilize host and generated-code contracts (A1, A4, A6).
5. Document and finalize `.zui` and unfinished feature contracts (A5, A16, A17).
