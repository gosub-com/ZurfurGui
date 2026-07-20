using ZurfurGui.Base;
using ZurfurGui.Controls;

namespace ZurfurGui.Render;

internal class ScrollBarRenderer : Renderable
{
    readonly ScrollBar _control;

    public ScrollBarRenderer(ScrollBar control)
    {
        _control = control;
    }

    public string RenderType => "ScrollBar";

    public void Render(View view, RenderContext context)
    {
        var data = _control.DataContext;
        var trackRect = new Rect(new(), view.Size);
        var orientationValue = view.GetStyle(ScrollBar.OrientationProperty);

        // Render track background
        var trackColor = view.GetStyle(ScrollBar.TrackColorProperty);
        context.FillRect(trackColor, trackRect);

        // Render border around track
        var borderColor = view.GetStyle(ScrollBar.BorderColorProperty);
        var borderWidth = view.GetStyle(ScrollBar.BorderWidthProperty);
        context.StrokeRect(new Pen(borderColor, borderWidth), trackRect);

        // Only render thumb if scrollable
        if (data.Maximum > data.Minimum)
        {
            // Render thumb with rounded corners
            var thumbRect = _control.CalculateThumbRect();
            var thumbColor = view.GetStyle(ScrollBar.ThumbColorProperty);
            var thumbRadius = view.GetStyle(ScrollBar.ThumbRadiusProperty);
            context.FillRect(thumbColor, thumbRect, thumbRadius);

            // Render arrow triangles
            var startArrowRect = _control.GetStartArrowRect();
            var startArrowPoints = GetArrowTrianglePoints(startArrowRect, orientationValue, true);
            context.FillPolygon(new Brush(thumbColor), startArrowPoints);

            var endArrowRect = _control.GetEndArrowRect();
            var endArrowPoints = GetArrowTrianglePoints(endArrowRect, orientationValue, false);
            context.FillPolygon(new Brush(thumbColor), endArrowPoints);
        }
    }

    /// <summary>
    /// Generate triangle points for an arrow within the given rectangle.
    /// Triangle is inset by 25% padding from edges.
    /// </summary>
    /// <param name="arrowRect">The rectangle containing the arrow</param>
    /// <param name="orientation">Scrollbar orientation</param>
    /// <param name="pointsToStart">True for start arrow (up/left), false for end arrow (down/right)</param>
    /// <returns>Array of triangle points as [x1, y1, x2, y2, x3, y3]</returns>
    double[] GetArrowTrianglePoints(Rect arrowRect, Orientation orientation, bool pointsToStart)
    {
        var padding = 0.25;  // 25% padding on each side
        var centerX = arrowRect.X + arrowRect.Width / 2;
        var centerY = arrowRect.Y + arrowRect.Height / 2;

        if (orientation == Orientation.Vertical)
        {
            // Vertical scrollbar: up or down arrow
            var leftX = arrowRect.X + arrowRect.Width * padding;
            var rightX = arrowRect.X + arrowRect.Width * (1 - padding);
            var tipY = pointsToStart 
                ? arrowRect.Y + arrowRect.Height * padding          // Points up
                : arrowRect.Y + arrowRect.Height * (1 - padding);   // Points down
            var baseY = pointsToStart
                ? arrowRect.Y + arrowRect.Height * (1 - padding)    // Base at bottom
                : arrowRect.Y + arrowRect.Height * padding;         // Base at top

            return
            [
                centerX, tipY,      // Tip (top or bottom)
                rightX, baseY,      // Right base
                leftX, baseY        // Left base
            ];
        }
        else
        {
            // Horizontal scrollbar: left or right arrow
            var topY = arrowRect.Y + arrowRect.Height * padding;
            var bottomY = arrowRect.Y + arrowRect.Height * (1 - padding);
            var tipX = pointsToStart
                ? arrowRect.X + arrowRect.Width * padding           // Points left
                : arrowRect.X + arrowRect.Width * (1 - padding);    // Points right
            var baseX = pointsToStart
                ? arrowRect.X + arrowRect.Width * (1 - padding)     // Base at right
                : arrowRect.X + arrowRect.Width * padding;          // Base at left

            return
            [
                tipX, centerY,      // Tip (left or right)
                baseX, bottomY,     // Bottom base
                baseX, topY         // Top base
            ];
        }
    }

    public bool IsHit(View view, Point point)
    {
        var p = view.toClient(point);
        return new Rect(new(), view.Size).Contains(p);
    }
}
