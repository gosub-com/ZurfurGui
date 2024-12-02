using ZurfurGui.Controls;
using ZurfurGui.Render;

namespace ZurfurGui;

public interface Controllable
{
    /// <summary>
    /// Unique type name of control
    /// </summary>
    string Type { get; }

    /// <summary>
    /// The main control view.  Each control must have a MainView, that is readonly (i.e. never changes)
    /// </summary>
    View View { get; }

    void Build();

    /// <summary>
    /// Returns the desired size of the control given the available screen size.
    /// Called by View.Measure, should call View.Measure on child views.
    /// Similar to MeasureOverride in WPF.
    /// The default is the same as measuring a panel or window.
    /// </summary>
    Size MeasureView(Size available, MeasureContext measure)
    {
        return Helper.MeasurePanel(View.Views, available, measure);
    }

    /// <summary>
    /// Arrange the controls children.
    /// Similar to ArrangeOverride in WPF.
    /// The default is the same as arranging a panel or window.
    /// </summary>
    Size ArrangeViews(Size final, MeasureContext measure)
    {
        return Helper.ArrangePanel(View.Views, final, measure);
    }

    /// <summary>
    /// Render the control
    /// </summary>
    void Render(RenderContext context) { }

    /// <summary>
    /// Returns TRUE if this control is opaque at the point
    /// </summary>
    bool IsHit(Point target) { return false; }
}

