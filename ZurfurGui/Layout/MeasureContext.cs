using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Controls;
using ZurfurGui.Platform;

namespace ZurfurGui.Layout;

public class MeasureContext
{
    OsContext _context;

    public MeasureContext(OsContext context)
    {
        _context = context; 
    }

    public double MeasureTextWidth(string fontName, double fontSize, string text)
    {
        // This normalizes the font sizes so we could cache the results
        // and re-use them even if the font size changes.
        // TBD: See if this can be used to speed things up.
        // _context.FontName = fontName;
        // _context.FontSize = 1000;
        // return _context.MeasureTextWidth(text) * 0.001 * fontSize;


        _context.FontName = fontName;
        _context.FontSize = fontSize;
        return _context.MeasureTextWidth(text);
    }

}
