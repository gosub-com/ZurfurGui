using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Render;

namespace ZurfurGui.Controls;

public partial class Panel : Controllable
{

    public Panel()
    {
        InitializeComponent();
    }

    public void LoadContent()
    {
        Loader.BuildViews(View, View.Properties.Get(Zui.Content));
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
