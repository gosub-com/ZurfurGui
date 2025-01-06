using Microsoft.JSInterop;
using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

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
        BrowserStart.Start(canvasId, TestApp.Main.MainApp);
    }

}