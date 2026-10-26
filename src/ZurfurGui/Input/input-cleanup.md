# Input Remaining issues

**NOTE:** This document is in a state of flux.  Some issues here have already been fixed.

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
the public `ClickRecognizer`, which captures on pointer-down and invokes its callback only when pointer-up occurs
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



# Gesture recognizer cleanup plan

This document lists the open bugs, design gaps, and simplifications in the gesture layer (`GestureArena`,
`IGestureRecognizer`, `GestureSessionContext`, `ClickRecognizer`, and the gesture hooks in `PointerRouter`).
Completed work is not listed. It covers: accept or reject during `CreateSession`, exception safety in session
callbacks, the `Candidate` object, the single end path in the arena, and removal of `GestureRecognizerLifecycle`
(recognizers are now attached with `View.AddGestureRecognizer` and detached with `View.RemoveGestureRecognizer`;
`IGestureRecognizer` is not `IDisposable`).

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

`CaptureFromGesture` clears the contact's press chain and captures the winning view, but views that were pressed
receive no notification. A child using raw `PointerDown` and `PointerUp` never learns its press was taken away.
Only the previous capture owner receives `PointerCaptureLost`.

How to fix:

- In `PointerRouter.CaptureFromGesture`, raise `PointerCaptureLost` on each view returned by `state.CancelPress()`
  that is not the new owner.
- Document the arbitration policy for raw `SetCapture` versus a pending gesture ("raw capture wins") in
  `gesture-recognizer.md` and in the `View.SetCapture` XML comments.

### 2. Down routing after a gesture accepts on pointer-down (bug fix)

Status: ***fixed***

If a recognizer accepts during `Start`, capture is taken, but `wasCaptured` in `PointerRouter.PointerInput` was
computed earlier, so the raw down still routes through the hit chain instead of the capture owner.

How to fix: re-read `state.CapturedView` after `GestureArena.Start` and route the raw down to the capture owner.
The alternative is to document the current behavior; the code fix is preferred because it matches the documented
"the capture owner receives down, move, and up" model.

### 3. Contact state cleanup after pointer-up for touch and pen (bug fix, verify first)

Status: ***fixed*** `PointerRouter` now exits the hover chain and removes the contact state on `Up` for touch and
pen, so it no longer depends on the platform sending a trailing `Leave`. A later `Leave` is harmless. Mouse keeps
its hover state after `Up`. WinForms touch and pen are not yet tested.

On `Up`, the router clears press state but does not call `RemoveContactState`. For a mouse this is intended because
hover persists. For touch or pen, a missing trailing `Leave` leaves a stale entry in `_contactStates` and stale hover
chain membership. With multi-contact gestures (item 5) stale entries could be mistaken for live contacts.

How to fix: verify that the WinForms and browser adapters always send `Leave` or `Cancel` after a touch or pen `Up`.
If they do not, remove the contact state on `Up` for non-mouse devices, after the hover notifications.

### 4. Views removed or hidden mid-gesture (bug fix)

Status: ***fixed*** Removal: `View.RemoveChild` calls `PointerRouter.CancelViewTree` while the
child is still attached. It cancels gesture candidates, releases capture (with `PointerCaptureLost`), and drops the
view and its descendants from the hover and press chains. Reparenting is a remove followed by an add, so it also
cancels. Hiding: when a view's `IsVisible` becomes false, `View` calls the same method, which covers its descendants
(and therefore hidden ancestors) through a parent-chain test. A view hidden before the arena starts cannot become a
candidate, because hit testing skips invisible views and their subtrees.

**necessary before pan/zoom**: a pan target removed during a drag or inertia must not keep running.

The arena holds candidate views and never checks that they are still in the tree. A removed view can still receive
gesture input and capture, and the click recognizer's `PreRenderFrame` subscription stays alive until the arena ends.
Controls do not remove recognizers in `OnDetach` because `OnDetach` also runs when a view is reparented.

How to fix:

- When a view is detached from the app window (not merely moved), ask the router to cancel any arena whose
  candidate or capture owner is the view or a descendant.
- At minimum, check `candidate.View.AppWindow is null` when dispatching and cancel that candidate.
- Inertia animations (item 7) must also stop when the target is detached.

## Design gaps for pan/zoom

### 5. The arena is single-contact (design gap, major)

**necessary before pan/zoom**

Each contact gets its own arena, sessions are created per contact, and capture is per contact. A pinch needs two
contacts to reach one session, and the winner needs both contacts captured.

How to do it:

- Add an optional session interface, for example `IMultiContactGestureSession`, with
  `OnContactAdded(GestureSessionContext context)` and `OnContactRemoved(PointerContact contact)`.
- When a new contact goes down, the router first looks for an existing arena (accepted or pending) whose candidate
  view is in the new contact's hit chain. If its session implements the interface, offer the contact to it rather
  than starting an independent arena.
- Winner capture then runs per contact: call `CaptureFromGesture` for each contact the winner owns.
- When one contact goes up and others remain, the session keeps running. The arena ends only when the last contact
  is up or canceled.
- Decide what happens when a second contact lands on a view whose arena already has a winner on a different view.
- Keep single-contact recognizers (click) unchanged: they simply do not implement the new interface.

Note that `CancelRecognizer`, `ClearCaptures`, and `ReleaseGestureCapture` currently assume one arena per contact
state; they must be revisited together with this item.

### 6. No auto-win and no configurable slop (design gap)

**necessary before pan/zoom**

A last remaining candidate does not win when the others reject. That is fine for click, but pan/zoom needs an
explicit rule and a slop distance.

- Keep the rule "a recognizer must accept explicitly" and document it.
- Add a pan slop property next to `LongClickMovementTolerance` in the app window properties, and use it from the
  pan recognizer. Do not hard-code it. Consider separate values for touch and mouse.

### 7. Pan, flick, and zoom need timestamps and a shared clock (design gap)

**necessary before pan/zoom**

`PointerContact` and `PointerEvent` carry no timestamp, and `PreRenderFrame` passes no elapsed time.

- Either add a timestamp to `PointerEvent` populated by the platform adapters, or add one shared clock on the app
  window used by every recognizer. Do not let each recognizer create its own `Stopwatch`.
- Use device positions (`Contact.DevicePosition`) for pan/zoom math.
- Change `ClickSession` to compare device positions rather than view-relative positions, so content moving under the
  pointer (a scrolling parent) does not make a click drift. The click currently tests release against
  `CandidateView.OriginRect`; verify this is in device space and stays correct while the view moves.

### 8. No wheel or trackpad-zoom event (design gap)

Status: ***partly done*** A separate wheel event and router path now exist. The remaining work is listed under
[Mouse Wheel Improvements](#mouse-wheel-improvements).

Desktop zoom usually comes from the wheel or a trackpad rather than two fingers. Add a wheel event (or a separate
wheel callback) to `OsPointerEvent5`, the router, and the browser and WinForms adapters. Decide whether it routes
through the preview and bubble phases and how a recognizer opts in. Keep it out of the arena, because a wheel has no
down/up lifetime. Not required for touch pan, but required for desktop zoom.

### 9. Mouse-button policy is not visible to recognizers (design gap)

**necessary before pan/zoom**

The session context exposes the initial `Contact`, including `Buttons` and `Modifiers`, but the click recognizer
accepts any button. Decide which buttons the click recognizer responds to (currently all), and make sure a pan
recognizer can filter by device, button, and modifier without the arena creating sessions it will always reject.
Consider an optional `IGestureRecognizer.CanStart(PointerContact)` check before `CreateSession`.

## Cleanups and documentation

### 10. Stop copying `PointerEvent` per session (cleanup)

The arena builds a new `PointerEvent` for each session and event, and the docs explain that `Handled` is
independent. Share one copy per dispatch (so gesture events cannot affect raw routing) and shorten the doc.

### 11. Minor cleanups (cleanup)

- `ClickRecognizer.ValidateArguments`: replace with `ArgumentNullException.ThrowIfNull` in the constructor.
- Long-click timer: move the `Stopwatch` and `PreRenderFrame` logic into a small internal helper that pan and
  flick can reuse, behind the shared clock from item 7.
- `GestureArena` candidate creation: avoid `ToArray()` of the recognizer list for every view on every pointer-down.
- `PointerRouter.PointerInput`: remove the double `ToList()` in `UpdateViewChain(previousHoverChain.ToList(), ...)`.
- Move-path allocations (`GetViewChain`, `SendPointerEvent`'s `views.ToList()`, hover and press snapshots): leave for
  a later performance pass, but do not add new per-move allocations in the pan/zoom recognizer.
- `PointerRouter.ClearPointerCapture` writes `Debug.WriteLine("CaptureLost")`; remove it.
- Fix the `PointerEvent` XML comment typo `View.ToClinet`.

### 12. Thread affinity and manual test coverage (doc only)

**necessary before pan/zoom**: pan/zoom adds timers and frame callbacks, so these rules must be written down first.

- State that gesture APIs, including `GestureSessionContext.Accept` and `Reject`, are UI-thread only. The arena is
  not thread-safe. Update `gesture-recognizer.md` and the XML comments on `GestureSessionContext`.
- There is no test project. Before pan/zoom, extend the TestApp input surface with a scrollable area containing a
  button and selectable text so click-versus-pan, capture loss, and detach-mid-gesture can be checked by hand.
  Consider a test project with a fake clock and fake router input once the clock exists.

### Mouse Wheel Improvements

The first wheel implementation (see `input-events.md`, "Wheel events", and `scroll-viewer.md`, "Mouse wheel") is
deliberately small. `PointerRouter.WheelInput` hit-tests, walks from the hit view toward the root, and gives the
`WheelEvent` to the first view that has a `Panel.PointerWheel` handler. That view consumes the wheel, and there is no
preview phase, no `Handled` flag, and no capture involvement. The items below are known limitations, listed with how
to implement each one and how hard it is. No code has been changed for them yet. Wheel input stays out of the gesture
arena, because a wheel has no down/up lifetime.

Suggested order: W1 and W2 first (they change handler semantics together), then W3, W4, and W5, with W6 through W8
as time allows.

#### W1. Handlers cannot decline the wheel (design gap)

Difficulty: **trivial**

Today the first view with a handler consumes the wheel, even a ScrollViewer that is already at the end of its range,
so an inner scroller never passes the wheel to an outer one. Most frameworks (WPF, Avalonia, the DOM) let a handler
decline so a parent can take the event.

How to implement:

- Add `public bool Handled { get; set; }` to `WheelEvent`. A record can have a settable property; it does not need
  inheritance.
- In `PointerRouter.WheelInput`, keep walking toward the root after a handler returns, and stop only when
  `ev.Handled` is true. Views with no handler are still skipped.
- In `ScrollViewer.OnPointerWheel`, set `Handled = true` only when the offset actually changed (compare the clamped
  `ScrollOffset` before and after). This gives chained scrolling at the end of the range.
- Decide the policy and then document it. If the current "never chains" behavior is still wanted, add a styled
  property on ScrollViewer (for example `wheelChaining`, default to the chosen behavior) instead of hard-coding it.
- Update the "Wheel events" section of `input-events.md` and the "Mouse wheel" section of `scroll-viewer.md`, which
  both say the wheel never reaches a parent handler.

Any new handler must set `Handled` when it consumes the wheel, otherwise the walk continues to its ancestors.

#### W2. No preview phase for ancestors (design gap)

Difficulty: **low** (after W1)

A parent such as a zoomable canvas cannot take the wheel before a child ScrollViewer sees it. The pointer events have
a preview phase for this; the wheel does not.

How to implement:

- Add `$previewPointerWheel` (`EventHandler<WheelEvent>`, `attachedProperty`) to `Panel.zui.json5`, next to
  `$pointerWheel`.
- In `PointerRouter.WheelInput`, build the full chain once. Run preview from the root toward the hit view, stopping
  when `Handled` is set, then run the bubble walk from W1 from the hit view toward the root.
- Keep both routes allocation-light: the chain list is the only allocation, as it is today.
- Document that preview handlers see the wheel even when the pointer is over a child that would handle it, and that
  handling it in preview suppresses the bubble route (same rule as the pointer events).

#### W3. Wheel ignores pointer capture (design gap)

Difficulty: **low to medium**, needs a policy decision first

The wheel always goes to the view under the pointer. That matches browsers and Win32, and is right for most
controls. Some interactions want the wheel to keep going to the captured control, for example scrolling a list while
drag-selecting in it.

How to implement:

- Decide the rule. The least surprising one is: if the mouse contact is captured, offer the wheel to the capture
  owner first (as a normal route with that single view), and fall back to the hit chain if it does not handle it.
- `OsWheelEvent` uses `ContactId` 0 and `Device` mouse, so look up the contact state the same way mouse pointer events
  do. Add a read-only lookup on the router for the capture owner of a contact key; do not create contact state for a
  wheel that arrives before any pointer event.
- This depends on W1, because the fall back needs to know whether the capture owner handled the event.
- Document the rule in `input-events.md` and in the `View.SetCapture` XML comments.

#### W4. Wheel hit testing over transparent areas (verify first, possible bug)

Difficulty: **trivial to low**

The wheel uses the same hit test as pointer events. A view with no control renderer and a background alpha of 16 or
less is not hit, so a wheel over an empty, transparent part of a ScrollViewer's content may find no target and do
nothing, even though that point is inside the viewer. This is unverified; check it in the TestApp first.

How to implement (if it reproduces), pick one:

- Give the ScrollViewer's viewport view `HitTest = Always` in `ScrollViewer.zui.json5` or its constructor. Children
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
