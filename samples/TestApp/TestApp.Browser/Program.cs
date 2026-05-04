using ZurfurGui.Browser;

namespace TestApp.Browser;


public static class Program
{
    /// <summary>
    /// Called from Javascript with args[0] as the canvas to draw on
    /// </summary>
    private static void Main(string[] args)
    {
        Console.WriteLine($"C# Main called args: '{string.Join(" ", args)}'");

        var canvasId = args[0];
        BrowserStart.Start(canvasId, ZurfurMain.MainApp);
    }

}