using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Render;

namespace ZurfurGui.Controls;

public class Panel : Controllable
{
    public string Type => "Zui.Panel";
    public override string ToString() => View.ToString();
    public View View { get; private set; }

    public Panel()
    {
        View = new(this);
    }

    public void Render(RenderContext context)
    {
        // TBD: Clear for now
        //
        // var BORDER_WIDTH = 2;
        // var r = new Rect(new(), View.Size);
        // context.LineWidth = BORDER_WIDTH;
        // context.StrokeColor = Colors.Yellow;
        // r = r.Deflate(BORDER_WIDTH/2);
        // context.StrokeRect(r);
    }

}
