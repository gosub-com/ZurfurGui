namespace TestApp;

using ZurfurGui;
using ZurfurGui.Base;
using ZurfurGui.Controls;
using ZurfurGui.Property;
using ZurfurGui.Windows;
using TestApp.Controls;
using TestApp.Test;


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

        app.ShowWindow(new DebugWindow(), "Debug Window", location: new PointProp(10,10));

        app.ShowWindow(new FormQbfWinTest(), "Qbf Win Test",
            location: new PointProp(95,195), 
            sizeRequst: new SizeProp(400,500));

        var swt1 = new FormSmallWinTest();
        app.ShowWindow(swt1, "Small Win Test",
            sizeRequst: new SizeProp(200, 200), 
            margin: new ThicknessProp(600, 175, 0, 0));
        swt1.bigButton.View.SetProperty(TextView.TextProperty, new (["*BUTTON*"]));

        app.ShowWindow(new FormSmallWinTest(), "Small Win Test",
            sizeRequst: new SizeProp(200, 200), 
            margin: new ThicknessProp(10, 10, 10, 10),
            align: new AlignProp(AlignHorizontal.Right, AlignVertical.Bottom));


        var searchWin = app.ShowWindow(new FormSearch(), "Search",
            margin: new ThicknessProp(10, 10, 10, 10),
            align: new AlignProp(AlignHorizontal.Left, AlignVertical.Bottom));
        searchWin.IsWindowWrappingVisible = false;

        app.ShowWindow(new FormMultiForm(), "Multi Form",
            margin: new ThicknessProp(10, 10, 10, 10),
            align: new AlignProp(AlignHorizontal.Right, AlignVertical.Top));

        app.ShowWindow(new StyleTestCard(), "StyleTest",
            location: new PointProp(380, 120),
            sizeRequst: new SizeProp(250, 280));

        app.ShowWindow(new FormTestComboBox(), "ComboBox Test",
            location: new PointProp(20, 150)
            );

    }

}
