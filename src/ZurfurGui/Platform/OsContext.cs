using System;
using System.Collections.Generic;
using System.Text;
using ZurfurGui.Base;

namespace ZurfurGui.Platform;

/// <summary>
/// Platform independent drawing context.  
/// </summary>
public interface OsContext
{
    /// <summary>
    /// Presents the render buffer to the screen.
    /// </summary>
    public void Present(OsRenderBuffer buffer);

    /// <summary>
    /// Measure the width of a string in pixels using the specified font and size.
    /// </summary>
    public double MeasureTextWidth(string fontName, double fontSize, string text);

    /// <summary>
    /// Marshal a string to the specified index.
    /// </summary>
    public void MarshalString(string? str, int index);

}
