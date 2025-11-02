namespace TestApp;

using ZurfurGui;
using ZurfurGui.Base;
using ZurfurGui.Property;
using ZurfurGui.Controls;


/// <summary>
/// Each project should have a partial ZurfurMain class with a MainApp method as the entry point.
/// </summary>
public static partial class ZurfurMain
{
    /// <summary>
    /// This function is called to initialize the application.  It should call InitializeControls first.
    /// </summary>
    public static void MainApp(AppWindow app)
    {
        InitializeControls();

        app.SetMainappWindow(new FormQbfWinTest());

        app.ShowWindow(new DebugWindow(), location: new PointProp(10,10));

        app.ShowWindow(new FormQbfWinTest(), 
            location: new PointProp(95,175), 
            sizeRequst: new SizeProp(400,500));

        var swt1 = new FormSmallWinTest();
        app.ShowWindow(swt1, 
            sizeRequst: new SizeProp(200, 200), 
            margin: new ThicknessProp(600, 175, 0, 0));
        swt1.button1.View.SetProperty(Zui.Text, new (["*BUTTON*"]));

        app.ShowWindow(new FormSmallWinTest(), 
            sizeRequst: new SizeProp(200, 200), 
            margin: new ThicknessProp(10, 10, 10, 10),
            align: new AlignProp(AlignHorizontal.Right, AlignVertical.Bottom));


        var searchWin = app.ShowWindow(new FormSearch(), 
            margin: new ThicknessProp(10, 10, 10, 10),
            align: new AlignProp(AlignHorizontal.Left, AlignVertical.Bottom));
        searchWin.IsWindowWrappingVisible = false;

        app.ShowWindow(new FormMultiForm(), 
            margin: new ThicknessProp(10, 10, 10, 10),
            align: new AlignProp(AlignHorizontal.Right, AlignVertical.Top));

    }

}
