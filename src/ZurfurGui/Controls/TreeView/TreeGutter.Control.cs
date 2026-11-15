using ZurfurGui.Base;
using ZurfurGui.Controls;
using ZurfurGui.Input;
using ZurfurGui.Property;
using ZurfurGui.Render;

namespace ZurfurGui.Controls.TreeView;

/// <summary>
/// How the expander of a node with children is drawn.
/// </summary>
public enum TreeExpanderStyle
{
    /// <summary>A box containing a plus (collapsed) or a minus (expanded).</summary>
    Box,

    /// <summary>A chevron pointing right (collapsed) or down (expanded).</summary>
    Chevron
}

/// <summary>
/// The tree style settings a <see cref="TreeGutter"/> needs to draw itself.  The tree reads them
/// from its own styled properties and creates one instance that is shared by all of its gutters.
/// Records compare by value, so the tree can tell when a style change needs a refresh.
/// </summary>
public sealed record TreeGutterStyle(
    double IndentWidth,
    bool ShowLines,
    bool ShowExpander,
    double LineWidth,
    TreeExpanderStyle ExpanderStyle);

/// <summary>
/// The left side of a tree node row.  It draws the connecting lines and the expander.  The
/// <see cref="TreeView{Item}"/> creates these and tells each one its position in the tree.
/// </summary>
internal sealed partial class TreeGutter : Renderable
{
    const double ExpanderSize = 9;

    // One entry per level from the root to this node.  True when that ancestor (or this node,
    // for the last entry) is the last child of its parent.
    bool[] _isLast = [true];
    bool _hasChildren;
    bool _isExpanded;
    TreeGutterStyle _style = new(16, true, true, 1, TreeExpanderStyle.Box);

    /// <summary>
    /// Creates the gutter.
    /// </summary>
    public TreeGutter()
    {
        InitializeControl();
        View.Render = this;
    }

    /// <summary>
    /// Sets what the gutter draws and resizes it to fit the depth of the node.
    /// </summary>
    public void SetState(bool[] isLast, bool hasChildren, bool isExpanded, TreeGutterStyle style)
    {
        _isLast = isLast;
        _hasChildren = hasChildren;
        _isExpanded = isExpanded;
        _style = style;
        View.SetProperty(Panel.SizeRequest, new SizeProp(isLast.Length * style.IndentWidth, default));
        View.InvalidateRender();
    }

    /// <summary>
    /// Returns true if the device position is over the expander of a node that has children.
    /// </summary>
    public bool IsExpanderHit(Point devicePosition)
    {
        if (!_hasChildren || !_style.ShowExpander)
            return false;
        var p = View.toClient(devicePosition);
        var left = (_isLast.Length - 1) * _style.IndentWidth;
        return p.X >= left && p.X < left + _style.IndentWidth && p.Y >= 0 && p.Y < View.Size.Height;
    }

    // Draws the connecting lines and the expander.
    RenderResult Renderable.Render(View view, RenderContext context, Rect clientClip)
    {
        var color = Properties.LineColor;
        if (color.A == 0)
            return RenderResult.Discard;

        var brush = new Brush(color);
        var indent = _style.IndentWidth;
        var height = view.Size.Height;
        var midY = height / 2;
        var depth = _isLast.Length - 1;
        var showExpander = _style.ShowExpander && _hasChildren;
        var half = showExpander ? ExpanderSize / 2 : 0;

        if (_style.ShowLines)
        {
            for (int level = 0; level <= depth; level++)
            {
                var x = level * indent + indent / 2;
                if (level < depth)
                {
                    if (!_isLast[level])
                        DrawVertical(context, brush, x, 0, height);
                    continue;
                }
                DrawVertical(context, brush, x, 0, midY - half);
                if (!_isLast[level])
                    DrawVertical(context, brush, x, midY + half, height);
                DrawHorizontal(context, brush, x + half, (level + 1) * indent, midY);
            }
        }

        if (showExpander)
            DrawExpander(context, brush, depth * indent + indent / 2, midY);

        return RenderResult.RenderedAll;
    }

    // Always false, so clicks go to the row behind the gutter.
    bool Renderable.IsHit(View view, HitTestContext context) => false;

    // Draws the box or chevron centered at the given point.
    void DrawExpander(RenderContext context, Brush brush, double cx, double cy)
    {
        var pen = new Pen(brush, _style.LineWidth);
        if (_style.ExpanderStyle == TreeExpanderStyle.Box)
        {
            var half = ExpanderSize / 2;
            context.StrokeRect(pen, cx - half, cy - half, ExpanderSize, ExpanderSize);
            context.StrokePolyLine(pen, [cx - 2.5, cy, cx + 2.5, cy]);
            if (!_isExpanded)
                context.StrokePolyLine(pen, [cx, cy - 2.5, cx, cy + 2.5]);
        }
        else if (_isExpanded)
            context.StrokePolyLine(pen, [cx - 3, cy - 1.5, cx, cy + 1.5, cx + 3, cy - 1.5]);
        else
            context.StrokePolyLine(pen, [cx - 1.5, cy - 3, cx + 1.5, cy, cx - 1.5, cy + 3]);
    }

    // Draws a solid vertical line segment.
    void DrawVertical(RenderContext context, Brush brush, double x, double y1, double y2)
    {
        if (y2 <= y1)
            return;
        var width = _style.LineWidth;
        context.FillRect(brush, x - width / 2, y1, width, y2 - y1);
    }

    // Draws a solid horizontal line segment.
    void DrawHorizontal(RenderContext context, Brush brush, double x1, double x2, double y)
    {
        if (x2 <= x1)
            return;
        var width = _style.LineWidth;
        context.FillRect(brush, x1, y - width / 2, x2 - x1, width);
    }
}
