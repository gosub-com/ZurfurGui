
using ZurfurGui.WinForms;

namespace TestApp.Windows;

internal static class WinMain
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        var control = MainView.CreateView();
        WinStart.StartRendering(control);
    }
}