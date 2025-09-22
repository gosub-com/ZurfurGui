using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZurfurGui.Base;
using ZurfurGui.Controls;

namespace ZurfurGui.Layout;

public interface Layoutable
{
    /// <summary>
    /// Layout type, e.g. "Row", "Column", "Text", etc.
    /// </summary>
    string TypeName { get; }

    /// <summary>
    /// Returns the desired size of the control given the available screen size.
    /// Called by View.Measure, should call View.Measure on child views.
    /// The size available accounts for margin, border, and padding.
    /// Similar to MeasureOverride in WPF.
    /// </summary>
    Size MeasureView(View view, MeasureContext measure, Size available);

    /// <summary>
    /// Arrange the view's children.  
    /// The view's Size, Position, and ContentRect are set before this function is called. 
    /// Similar to ArrangeOverride in WPF.
    /// </summary>
    void ArrangeViews(View view, MeasureContext measure);

}
