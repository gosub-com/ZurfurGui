# ScrollViewer

`ScrollViewer` displays content through a clipped viewport and allows the user or application code to
change the visible offset with horizontal and vertical scrollbars.

The current implementation is an early control. It supports the core content, measurement, clipping,
offset, and scrollbar-range behavior, but several standard GUI features remain unfinished.

## How to use ScrollViewer

A ScrollViewer is a container control. Put the content to be scrolled in its `$content` section, just
as content is supplied to a `Panel` or another container control.

```json5
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

```json5
{
    $controller: "Panel",
    $layout: "Dock",
    $content: [
        {
            $layout: "Row",
            "dock.align": "top",
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

Scrollbar values and `ScrollOffset` are synchronized in both directions. Internal synchronization is
guarded so updating one side does not recursively update the other.

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

`visibility` uses `ScrollBarVisibility`:

- `Visible` always displays the scrollbar.
- `Hidden` does not display the scrollbar, but the range and value can still be used programmatically.
- `Auto` displays the scrollbar only when its calculated maximum is greater than its minimum.

The current default is `Auto`, so scrollbars are hidden when their content fits and shown when their content
overflows. Applications can set a different policy through the scrollbar data when needed.

## Overlay scrollbar behavior

ScrollViewer uses overlay scrollbars. The scrollbars are drawn above the clipped content and do not consume
viewport space. Showing or hiding either scrollbar therefore does not change the content width, content height,
or the content extent used to calculate the scroll range.

The intended visibility behavior is:

- `Auto` hides a scrollbar when its content fits and shows it when its maximum range is greater than zero.
- `Visible` shows the scrollbar even when its range is zero. In that state it has no scrollable thumb movement.
- `Hidden` does not render the scrollbar, but its range and value remain available to application code.

When only one scrollbar is visible, it uses the full corresponding edge of the viewport. When both are visible,
the horizontal scrollbar is shortened by the vertical scrollbar's thickness and the vertical scrollbar is
shortened by the horizontal scrollbar's thickness. This leaves an empty, non-scrollable corner where the two
bars meet. No corner control or gutter is added yet.

Scrollbar visibility is evaluated from the content extent and the unchanged viewport size. The appearance of a
scrollbar does not reduce the size used to measure content, so visibility does not require the iterative
two-axis measurement used by traditional scrollbars that consume layout space.

ScrollViewer configures its internal scrollbars with `expandOnHover: true`. When an overflowing scrollbar is not
hovered, it renders only a thin translucent thumb near the outer edge. The thumb keeps its normal calculated
position and length along the scroll direction, so it indicates the portion of content currently visible without
covering the full content edge. The scrollbar's full outer rectangle remains available as an invisible pointer
target.

When the pointer enters that outer rectangle, the scrollbar renders its partially opaque track, opaque thumb, and
arrow indicators. The thumb becomes thicker and moves toward the center of the scrollbar. Pressing or dragging also
uses the expanded appearance. Arrow space remains reserved even while the arrow indicators are not drawn, so the
scroll range and layout do not change between visual states.

Scrollbar theme colors use the effective `ScrollBar.isExpanded` condition rather than relying only on
`isPointerOver`. This keeps colors synchronized with the expanded geometry during hover, press, and drag states.

The standard range model is:

```plaintext
Minimum = 0
Maximum = max(0, contentExtent - viewportExtent)
Value   = clamped current offset
```

When content fits in an axis, that axis has a maximum of zero and no scrollable thumb is rendered.

## Sizing and limitations

The ScrollViewer has no default `$sizeRequest` or `$sizeMax`. Its content is measured separately so it can be larger
than the viewport. The viewer reports the smaller of the content's natural size and the available parent size.

When content is smaller than the available parent size, the ScrollViewer sizes to the content. When content is larger,
the viewer uses the available size as its finite viewport and scrollbars represent the overflow. If a parent measures
with unconstrained space, the ScrollViewer may grow toward its content because no finite viewport has been established.
Applications should provide a finite constraint through the viewer's parent when a bounded viewport is required.

Other current limitations include:

- No mouse-wheel scrolling.
- No keyboard scrolling.
- No touch or pointer panning of content.
- No accessibility metadata or automation support.
- No nested-scroll input routing.
- No configurable content alignment; content starts at the top-left.
- No configurable scrollbar gutter or overlay inset.
- The overlay corner is currently empty and is not yet represented by a dedicated corner control.
- There are no convenience `HorizontalOffset` or `VerticalOffset` properties yet.
- Extent and viewport sizes are not currently exposed as public ScrollViewer properties.

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

## Content loading implementation

The loader distinguishes between a control's own template content and content supplied by its parent. The calls occur
in this order:

```text
ScrollViewer constructor
        |
        +--> Loader.Load(ScrollViewer, ScrollViewer's own ZUI JSON)
        |       |
        |       +--> LoadTemplateContent(template content)
        |               |
        |               +--> add _contentViewport to ScrollViewer.View
        |               +--> add horizontalScrollBar to ScrollViewer.View
        |               +--> add verticalScrollBar to ScrollViewer.View
        |               +--> _contentWindow is created under _contentViewport
        |
        +--> generated named fields are initialized
        +--> ScrollViewer-specific layout and scrollbar setup completes

Parent creates the ScrollViewer from its $content
        |
        +--> Loader.CreateControl(ScrollViewer properties, context)
                |
                +--> construct ScrollViewer and complete the sequence above
                +--> LoadParentContent(parent content)
                        |
                        +--> ContentHost returns _contentWindow.View
                        +--> default implementation adds user content there
```

`Loader.Load` invokes `LoadTemplateContent` for the ScrollViewer's own ZUI children. These children create the content
viewport, content window, and scrollbars directly on the ScrollViewer. Later, `Loader.CreateControl` invokes the
default `LoadParentContent` implementation for the parent's content. ScrollViewer's `ContentHost` points to
`_contentWindow`, so application content is added inside the clipped content subtree.

`LoadTemplateContent` and `LoadParentContent` both have default implementations. The former adds internal content to
the main view, while the latter adds parent content to `ContentHost`, which is the main view by default. ScrollViewer
only needs to override the template hook and content host; it no longer infers the loading phase from
`View.Children.Count`.

The separate lifecycle is also useful for future controls with templates or named content hosts. Those controls can
build their internal structure through `LoadTemplateContent` and direct ordinary parent content through an appropriate
content host without adding special cases to the loader.

## Measurement and arrangement

The ScrollViewer uses a dedicated layout implementation. The content viewport also uses a dedicated layout
implementation because ordinary panel measurement would constrain the content window to the viewport.

During measurement:

1. The ScrollViewer measures its direct children using its custom layout.
2. The content viewport measures `_contentWindow` with positive-infinite width and height.
3. The content window and descendants report their natural desired extent.
4. The viewport reports the smaller of the natural content size and the available parent size.
5. The scrollbars are measured as overlay children and do not reduce the viewport size.

During arrangement:

1. The ScrollViewer's children are arranged in the viewer's content rectangle.
2. The content viewport occupies the viewer's viewport area.
3. The content window is arranged at its natural `DesiredTotalSize`, which can exceed the viewport.
4. The viewport clips the larger content window.
5. The horizontal and vertical scrollbars are arranged as overlay siblings.
6. Scrollbar ranges are synchronized using the content window's desired content size and the viewport size.

The content window's normal `$offset` is used as the physical translation mechanism. A public
`ScrollOffset` of `(x, y)` applies a content-window offset of `(-x, -y)`. This moves the entire content
subtree while leaving the clipped viewport stationary.

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

Range synchronization also applies the scrollbar visibility policy. With the default `Auto` policy, a bar is
hidden when its measured range is zero. An explicitly `Visible` bar remains displayed in that state, while the
renderer omits its thumb and arrow indicators because there is no scrollable range in that axis.

## What has been implemented

The current implementation includes:

- A generated ScrollViewer control and ZUI structure.
- Normal parent `$content` support.
- A separate clipped content viewport.
- A content window that hosts all user content.
- Natural content measurement using unbounded measurement for the content window.
- Arrangement of content at its natural desired size.
- Content translation through the existing `$offset` property.
- Public `ScrollOffset` access.
- Horizontal and vertical scrollbar instances exposed by generated names.
- Scrollbar range synchronization.
- Safe clamping of values and offsets without exceptions.
- Programmatic offset changes through the sample's `Offset +` and `Offset -` buttons.
- Scrollbar value changes connected back to `ScrollOffset`.
- Auto scrollbar visibility based on content overflow.

## Remaining work

The following work is intentionally left for later:

1. Add `HorizontalOffset`, `VerticalOffset`, `ExtentWidth`, `ExtentHeight`, `ViewportWidth`, and
   `ViewportHeight` convenience properties.
2. Add scrollbar auto-repeat, keyboard input, mouse-wheel scrolling, touch panning, and nested-scroll
   event routing.
3. Add accessibility and automation metadata for orientation, range, value, and viewport size.
4. Add configurable top-left, centered, and other content alignment behavior.
5. Improve loader lifecycle APIs so internal control content and parent content are distinct operations.
6. Add tests for content that fits, content that overflows horizontally, content that overflows vertically,
   content that overflows in both directions, and content that shrinks after scrolling.

## Current continuation point

The current implementation has completed content loading, clipping, content-sized measurement,
offset translation, scrollbar range synchronization, and scrollbar-value synchronization.

The next validation target is the `FormTestScrollViewer` sample. Verify horizontal movement,
vertical overflow, thumb rendering, range clamping, and overlay placement.

The sample now relies on the ScrollViewer's content-sized measurement. Its content may not exceed the
available viewport in one axis, so a missing scrollbar may be expected rather than a control failure.

The next likely implementation task is configurable overlay gutter behavior.
Do not begin mouse-wheel, keyboard, touch, accessibility, or nested-scroll work until the
two-axis layout behavior is stable.