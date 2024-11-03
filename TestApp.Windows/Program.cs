
using ZurfurGui.WinForms;

namespace TestApp.Windows;

internal static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        var control = MainView.CreateMainView();
        WinStart.Start(control);
    }
}