using ZurfurGui.Render;

namespace ZurfurGui.Controls;

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


    View BuildView(Properties properties);

    /// <summary>
    /// Returns the desired size of the control given the available screen size.
    /// Called by View.Measure, should call View.Measure on child views.
    /// Similar to MeasureOverride in WPF
    /// </summary>
    Size MeasureView(Size available, MeasureContext measure);

    /// <summary>
    /// Arrange the controls children.
    /// Similar to ArrangeOverride in WPF
    /// </summary>
    Size ArrangeViews(Size final, MeasureContext measure);

    /// <summary>
    /// Render the control
    /// </summary>
    void Render(RenderContext context) { }
}

