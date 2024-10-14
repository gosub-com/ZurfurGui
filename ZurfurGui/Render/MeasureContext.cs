using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Controls;
using ZurfurGui.Platform;

namespace ZurfurGui.Render;

public class MeasureContext
{
    OsContext _context;

    public MeasureContext(OsContext context)
    {
        _context = context; 
    }

    public double MeasureTextWidth(string fontName, double fontSize, string text)
    {
        _context.FontName = fontName;
        _context.FontSize = 1000;
        return _context.MeasureTextWidth(text) * 0.001 * fontSize;
    }

}
