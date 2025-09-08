using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Controls;

namespace ZurfurGui.Draw;
public class DrawButton : Drawable
{
    /// <summary>
    /// Since there is no state, we can use a single instance for all text
    /// </summary>
    public static readonly DrawButton Instance = new();


    public string DrawType => "Button";


    public void Draw(View view, DrawContext context, Rect contentRect)
    {
        // Draw background
        //context.FillColor = View.PointerHoverTarget ? Colors.Red : Colors.Gray;
        //context.FillRect(0, 0, View.Size.Width, View.Size.Height);
        view.Properties.Set(Zui.Background, view.PointerHoverTarget ? Colors.Red : Colors.Gray);
        var color = Colors.White;
        DrawLabel.Draw(view, context, contentRect, color);
    }

    public bool IsHit(View view, Point point)
    {
        var p = view.toClient(point);
        return new Rect(new(0, 0), view.Size).Contains(p);
    }

}
