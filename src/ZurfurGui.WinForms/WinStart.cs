using ZurfurGui.Windows;

namespace ZurfurGui.WinForms;

/// <summary>
/// Provides the entry point for starting ZurfurGui with the Windows Forms host. See
/// <see href="../../docs/architecture.md">docs/architecture.md</see> for platform architecture details.
/// </summary>
public static class WinStart
{
    /// <summary>
    /// Initializes the Windows Forms host and runs the application window.
    /// </summary>
    /// <param name="mainAppEntry">The callback used to initialize the application window.</param>
    public static void Start(Action<AppWindow> mainAppEntry)
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        Application.Run(new FormZurfurGui(mainAppEntry));
    }
}
