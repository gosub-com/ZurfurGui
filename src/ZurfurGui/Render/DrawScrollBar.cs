using ZurfurGui.Base;
using ZurfurGui.Controls;

namespace ZurfurGui.Render;

public class DrawScrollBar : Drawable
{
    readonly ScrollBar _control;

    public DrawScrollBar(ScrollBar control)
    {
        _control = control;
    }

    public string DrawType => "ScrollBar";

    public void Draw(View view, RenderContext context)
    {
        // Quick exit when drawing outside the clip region
        if (context.DeviceClip.Intersect(view.toDevice(view.ContentRect)).Width == 0)
            return;

        var data = _control.DataContext;
        var trackRect = new Rect(new(), view.Size);

        // Draw track background
        var trackColor = view.GetStyle(ScrollBar.TrackColorProperty);
        context.FillColor = trackColor;
        context.FillRect(trackRect);

        // Draw border around track
        var borderColor = view.GetStyle(ScrollBar.BorderColorProperty);
        var borderWidth = view.GetStyle(ScrollBar.BorderWidthProperty);
        context.StrokeColor = borderColor;
        context.LineWidth = borderWidth;
        context.StrokeRect(trackRect);

        // Only draw thumb if scrollable
        if (data.Maximum > data.Minimum)
        {
            // Draw thumb with rounded corners
            var thumbRect = _control.CalculateThumbRect();
            var thumbColor = view.GetStyle(ScrollBar.ThumbColorProperty);
            var thumbRadius = view.GetStyle(ScrollBar.ThumbRadiusProperty);
            context.FillColor = thumbColor;
            context.FillRect(thumbRect, thumbRadius);
        }
    }

    public bool IsHit(View view, Point point)
    {
        var p = view.toClient(point);
        return new Rect(new(), view.Size).Contains(p);
    }
}
