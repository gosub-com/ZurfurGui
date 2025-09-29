using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Base;
using ZurfurGui.Controls;

namespace ZurfurGui.Components;

public partial class FormSmallWinTest : Controllable
{
    // TBD: Code generated
    public Button button1;
    public Button button2; 

    public FormSmallWinTest()
    {
        InitializeControl();

        // TBD: Code generated
        button1 = (Button)View.FindByName("button1").Controller;
        button2 = (Button)View.FindByName("button2").Controller;
    }
}
