# ScrollViewer

## Overview

`ScrollViewer` displays content through a clipped viewport and allows the user or application code to
change the visible offset with horizontal and vertical scrollbars.

The current implementation is an early control. It supports the core content, measurement, clipping,
offset, and scrollbar-range behavior, but several standard GUI features remain unfinished.

## How to use ScrollViewer

A ScrollViewer is a container control. Put the content to be scrolled in its `$content` section, just
as content is supplied to a `Panel` or another container control.

```
{
    $controller: "ScrollViewer",
    $content: [
        {
            $controller: "Panel",
            $content: [
                {
                    $controller: "TextView",
                    text: "Scrollable content"
                }
            ]
        }
    ]
}
```

The ScrollViewer should normally receive its size from its parent layout. The scrollable content should
not normally have a `$sizeRequest`, because its natural measured size is used to calculate the scroll
range.

A parent layout must provide a finite viewport when the application needs a bounded scrolling area. For
example, a dock layout can place a toolbar at the top and make the final ScrollViewer child fill the
remaining area:

```
{
    $controller: "Panel",
    $layout: "Dock",
    $content: [
        {
            $layout: "Row",
            dock.align: "top",
            $content: [
                {
                    $controller: "TextView",
                    text: "Toolbar"
                }
            ]
        },
        {
            $controller: "ScrollViewer",
            $content: [
                {
                    $controller: "Panel",
                    $content: [
                        // Content that may be larger than the viewport.
                    ]
                }
            ]
        }
    ]
}
```

The final visible child of the current `Dock` layout receives the remaining space. The ScrollViewer
should therefore be the last child when it is intended to fill the area below a top row.

## Public control access

The generated ScrollViewer controller exposes the named scrollbar controls:

```csharp
public ScrollBar horizontalScrollBar;
public ScrollBar verticalScrollBar;
```

It also exposes the combined offset:

```csharp
public Point ScrollOffset { get; set; }
```

The offset is expressed in content coordinates. Increasing `X` moves the content left, and increasing
`Y` moves the content up, revealing content farther to the right or below the viewport.

For example:

```csharp
var offset = scrollViewer.ScrollOffset;
scrollViewer.ScrollOffset = new Point(offset.X, offset.Y + 8);
```

The setter clamps both coordinates to the current scrollbar ranges. Setting an offset outside the valid
range does not throw; it is normalized to the nearest valid value.

Changing a scrollbar's `DataContext.Value` has the same effect:

```csharp
scrollViewer.verticalScrollBar.DataContext.Value += 8;
```

Scrollbar values and `ScrollOffset` are synchronized in both directions.

## Scrollbar data and visibility

Each scrollbar has the following range-related data:

| Property | Meaning |
|---|---|
| `minimum` | The minimum offset, normally `0`. |
| `maximum` | The largest valid offset, calculated as content extent minus viewport extent. |
| `value` | The current offset along the scrollbar's orientation. |
| `viewportSize` | The visible size used to calculate the thumb size. |
| `smallChange` | The small movement amount; the default is currently `0` unless configured. |
| `largeChange` | The page movement amount; the viewer sets it to the viewport size. |
| `visibility` | Whether the viewer displays the scrollbar automatically, always, or never. |

`visibility` uses `ScrollBarVisibility`; its default is `Auto`. The overlay section below describes how
each visibility setting affects rendering.

- `Visible` always displays the scrollbar.
- `Hidden` does not display the scrollbar, but the range and value can still be used programmatically.
- `Auto` displays the scrollbar only when its calculated maximum is greater than its minimum.

## Overlay scrollbar behavior

ScrollViewer uses overlay scrollbars. The scrollbars are drawn above the clipped content and do not
consume viewport space. Showing or hiding either scrollbar therefore does not change the content width,
content height, or the content extent used to calculate the scroll range.

When only one scrollbar is visible, it uses the full corresponding edge of the viewport. When both are
visible, the horizontal scrollbar is shortened by the vertical scrollbar's thickness and the vertical
scrollbar is shortened by the horizontal scrollbar's thickness. This leaves an empty, non-scrollable
corner where the two bars meet. No corner control or gutter is added yet.

`Auto` hides a bar with no scrollable range, `Visible` keeps it rendered, and `Hidden` suppresses it
without removing its range or value.

Scrollbar visibility is evaluated from the content extent and the unchanged viewport size. The
appearance of a scrollbar does not reduce the size used to measure content, so visibility does not
require the iterative two-axis measurement used by traditional scrollbars that consume layout space.

The standard range model is:

```plaintext
Minimum = 0
Maximum = max(0, contentExtent - viewportExtent)
Value   = clamped current offset
```

When content fits in an axis, that axis has a maximum of zero and no scrollable thumb is rendered.

## Mouse wheel

ScrollViewer handles the mouse wheel. It moves the content by `wheelSteps` scroll steps for each wheel
detent. One step is `scrollSize` (16 pixels by default), so the default is 3 x 16 = 48 pixels per
detent.
Fractional wheel deltas from precision touchpads scroll proportionally.

`wheelSteps` is a styled property. It comes from the `scroll.wheel.steps` variable in the base theme
(`ZurfurBase`, default 3) and can be overridden on an individual ScrollViewer:

```
{
	$controller: "ScrollViewer",
	wheelSteps: 5
}
```

- Wheel over any part of the ScrollViewer works, including its content, empty space, and its
  scrollbars.
- Shift+wheel scrolls horizontally when the device reports only vertical movement.
- Horizontal wheel events scroll horizontally.
- Ctrl is ignored by the ScrollViewer. The browser and the WinForms form use Ctrl+wheel for
  magnification, so the platform adapters do not forward it.
- An axis disabled by `scrollMode` ignores its wheel movement.
- The offset is clamped like any other `ScrollOffset` change.
- The ScrollViewer sets `Handled` whenever it can scroll in the wheel's direction according to
  `scrollMode`, even at the end of its range, so a ScrollViewer inside another ScrollViewer does not let
  the parent see that wheel. If it cannot scroll in the wheel's direction (for example a vertical wheel
  over a horizontal-only viewer), it leaves the event unhandled so a parent can scroll. Ctrl is always
  marked handled. Controls inside the viewer can handle the wheel first by setting `Handled` themselves.

Wheel deltas follow the browser's pixel-mode convention: one detent is 100 units, positive Y scrolls
down and positive X scrolls right. Scrolling is therefore `notches * wheelSteps * scrollSize`, added to
`ScrollOffset`.

## Panning and flick

- Touch and pen pan with any contact. The mouse pans only with the right button by default.
- Panning moves `ScrollOffset`, clamped to the scrollbar ranges. The content does not jump when the pan
  starts; the slop distance is ignored.
- On release, a touch or pen contact flicks: the content keeps moving and slows down. The mouse never
  flicks. The viewer stops the flick at the scroll limit on each axis, and touching the viewer during a
  flick stops it without clicking the content under the finger. The wheel, scrollbar changes, and a new
  pan also stop it.
- The viewer pans only on axes it can scroll according to `scrollMode`. A viewer that cannot scroll at
  all does not take part, so an ancestor viewer pans instead. A viewer that can scroll an axis takes the
  pan when movement along that axis passes the slop, even when it is at its limit.

Configure it through `ScrollViewer.PanOptions`:

```csharp
scrollViewer.PanOptions.Slop = 1;                              // Pan almost immediately
scrollViewer.PanOptions.MouseButtons = PointerButtons.Left;    // Left button pans with the mouse
scrollViewer.PanOptions.Flick = false;                         // Stop when released
```

`Slop` is in layout units (scaled by the DPR) and defaults to the app window's movement slop for the
device.

## Sizing and known limitations

The ScrollViewer has no default `$sizeRequest` or `$sizeMax`. Its content is measured separately so it
can be larger than the viewport. The viewer reports the smaller of the content's natural size and the
available parent size.

When content is smaller than the available parent size, the ScrollViewer sizes to the content. When
content is larger, the viewer uses the available size as its finite viewport and scrollbars represent
the overflow. If a parent measures with unconstrained space, the ScrollViewer may grow toward its content
because no finite viewport has been established. Applications should provide a finite constraint
through the viewer's parent when a bounded viewport is required.

Current limitations include:

- No keyboard scrolling.
- No accessibility metadata or automation support.
- No configurable content alignment; content starts at the top-left.
- No configurable scrollbar gutter or overlay inset.
- The overlay corner is currently empty and is not yet represented by a dedicated corner control.
- There are no convenience horizontal/vertical offset or extent/viewport properties.

<!-- INTERNAL -->

## Implementation Section

### Overlay rendering

ScrollViewer configures its internal scrollbars with `expandOnHover: true`. When an overflowing
scrollbar is not hovered, it renders only a thin translucent thumb near the outer edge. The thumb keeps
its normal calculated position and length along the scroll direction, so it indicates the portion of
content currently visible without covering the full content edge. The scrollbar's full outer rectangle
remains available as an invisible pointer target.

When the pointer enters that outer rectangle, the scrollbar renders its partially opaque track, opaque
thumb, and arrow indicators. The thumb becomes thicker and moves toward the center of the scrollbar.
Pressing or dragging also uses the expanded appearance. Arrow space remains reserved even while the
arrow indicators are not drawn, so the scroll range and layout do not change between visual states.

Scrollbar theme colors use the effective `ScrollBar.isExpanded` condition rather than relying only on
`isPointerOver`. This keeps colors synchronized with the expanded geometry during hover, press, and drag
states.

### Mouse-wheel routing

The ScrollViewer subscribes to the `Panel.PointerWheel` event. The wheel is not part of the pointer
preview and bubble routing. Instead, the router gives the event to the first view with a `PointerWheel`
handler, starting at the view under the pointer and walking toward the root, until a handler sets
`WheelEvent.Handled`. See [Input events](input-events.md).

The ScrollViewer sets `HitTest` to `Always`, so it is the hit target for any point in its viewport
that no child claims. This includes empty space and gaps between transparent children.

### Panning and flick

The ScrollViewer owns a `PanRecognizer` (see [Gesture recognizer](gesture-recognizer.md)) that takes
part in the gesture arena, so buttons and other controls inside the viewer still receive clicks. A
click wins if the contact is released before it moves past the slop; the pan wins if it moves past the
slop first, which cancels the child's click and captures the contact for the viewer.

The ScrollViewer, not the `PanRecognizer`, owns the flick. The recognizer only reports the release
velocity (measured from the last 100 ms of finger movement) and applies no motion after release. The
viewer converts that velocity to scroll units and animates it on each frame (`PreRenderFrame`): the
speed decays exponentially (friction of 3 per second), the frame time is capped at 50 ms so a stalled
frame does not jump, the flick ends when the speed drops below 10 units per second, and an axis stops
as soon as the offset reaches its minimum or maximum. These values are constants in the viewer and are
not yet configurable. `PanOptions.Flick`, `MinFlickVelocity`, and `MaxFlickVelocity` only control
whether and how fast a release reports a velocity.

## Current visual structure

The ScrollViewer's generated ZUI structure is:

```plaintext
ScrollViewer
├── _contentViewport
│   └── _contentWindow
├── horizontalScrollBar
└── verticalScrollBar
```

`_contentViewport` is a `Panel` with `$clip: true`. The content window and its descendants are inside
that clipped subtree. The scrollbars are siblings of the viewport, so they remain visible above the
content instead of being clipped with it.

The content window is the control-owned host for user content. User content supplied through the
ScrollViewer's parent `$content` is placed in this host rather than directly beside the scrollbars.

For the distinction between template content and parent-supplied content, see
[Initialization](initialization.md).

## Measurement and arrangement

The ScrollViewer uses a dedicated layout implementation. The content viewport also uses a dedicated
layout implementation because ordinary panel measurement would constrain the content window to the
viewport.

During measurement:

1. The ScrollViewer measures its direct children using its custom layout.
2. The content viewport measures `_contentWindow` with positive-infinite width and height.
3. The content window and descendants report their natural desired extent. If it uses the default Panel layout,
   child `Panel.Offset` values contribute to its measured right and bottom extents.
4. The viewport reports the smaller of the natural content size and the available parent size.
5. The scrollbars are measured as overlay children and do not reduce the viewport size.

During arrangement:

1. The ScrollViewer's children are arranged in the viewer's content rectangle.
2. The content viewport occupies the viewer's viewport area.
3. The content window is arranged at its natural `DesiredTotalSize`, which can exceed the viewport.
4. The viewport clips the larger content window.
5. The horizontal and vertical scrollbars are arranged as overlay siblings.
6. Scrollbar ranges are synchronized using the content window's desired content size and the viewport size.

The content window's `$visualOffset` (`Panel.VisualOffset`) provides physical translation.
`ScrollOffset` of `(x, y)` moves the content window by `(-x, -y)`; the viewport remains fixed.
`VisualOffset` does not change the content window's measured extent.

The default Panel layout keeps its origin at `(0, 0)`. Negative child `Panel.Offset` values do not
extend content extent before that origin and may be clipped. Custom layouts must account for
`Panel.Offset` themselves if positioned children should contribute to their measured extent.

## Range synchronization

After arrangement, the ScrollViewer calculates each scrollbar independently:

```plaintext
content extent  = content window desired size along the axis
viewport extent = clipped viewport size along the axis
maximum        = max(0, content extent - viewport extent)
```

It then updates `Minimum`, `Maximum`, `ViewportSize`, and `LargeChange`. The current offset is clamped
after the range is updated. This ensures that shrinking content cannot leave the content window displaced
past its new maximum.

Scrollbar values and `ScrollOffset` are synchronized in both directions. A guard prevents updating one
side from recursively updating the other.

Range synchronization also applies the scrollbar visibility policy. With the default `Auto` policy, a
bar is hidden when its measured range is zero. An explicitly `Visible` bar remains displayed in that
state, while the renderer omits its thumb and arrow indicators because there is no scrollable range in
that axis.
