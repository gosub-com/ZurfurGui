using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Layout;
using ZurfurGui.Draw;

namespace ZurfurGui.Controls;

public partial class Button : Controllable
{

    public Button()
    {
        InitializeControl();
    }

    public Layoutable DefaultLayout => LayoutText.Instance;
    public Drawable? DefaultDraw => DrawButton.Instance;

}
