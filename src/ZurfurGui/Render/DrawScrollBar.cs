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
        var trackColor = view.GetStyle(ScrollBar.TrackColor);
        context.FillColor = trackColor;
        context.FillRect(trackRect);

        // Draw border around track
        var borderColor = view.GetStyle(ScrollBar.BorderColor);
        var borderWidth = view.GetStyle(ScrollBar.BorderWidth);
        context.StrokeColor = borderColor;
        context.LineWidth = borderWidth;
        context.StrokeRect(trackRect);

        // Only draw thumb if scrollable
        if (data.Maximum > data.Minimum)
        {
            var thumbRect = _control.CalculateThumbRect();

            // Choose thumb color based on interaction state
            Color thumbColor;
            if (view.GetProperty(Panel.IsPressed))
            {
                thumbColor = view.GetStyle(ScrollBar.ThumbPressedColor);
            }
            else if (view.GetProperty(Panel.IsPointerOver))
            {
                thumbColor = view.GetStyle(ScrollBar.ThumbHoverColor);
            }
            else
            {
                thumbColor = view.GetStyle(ScrollBar.ThumbColor);
            }

            // Draw thumb with rounded corners
            const double THUMB_RADIUS = 4.0;
            context.FillColor = thumbColor;
            context.FillRect(thumbRect, THUMB_RADIUS);
        }
    }

    public bool IsHit(View view, Point point)
    {
        var p = view.toClient(point);
        return new Rect(new(), view.Size).Contains(p);
    }
}
