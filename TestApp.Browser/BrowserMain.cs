using Microsoft.JSInterop;
using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

using ZurfurGui.Browser;

namespace TestApp.Browser;


public static class BrowserMain
{
    private static void Main(string[] args)
    {
        Console.WriteLine($"C# Main called args: '{string.Join(" ", args)}'");
        BrowserStart.StartRendering(args[0]);
    }

}