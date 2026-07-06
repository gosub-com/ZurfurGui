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
    public double MeasureTextWidth(string fontName, double fontSize, string text);

    public void MarshalString(string? str, int index);

    public void DrawBuffer(OsDrawBuffer buffer);
}
