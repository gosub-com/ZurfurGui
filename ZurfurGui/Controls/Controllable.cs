using ZurfurGui.Render;

namespace ZurfurGui.Controls;

public interface Controllable
{
    /// <summary>
    /// Unique type name of control
    /// </summary>
    string Type { get; }

    /// <summary>
    /// Optional control name ("" if not supplied).  When supplied, must be unique within the controls collection.
    /// </summary>
    string Name { get; init; }

    /// <summary>
    /// The main control view.  Each control must have a MainView, that is readonly (i.e. never changes)
    /// </summary>
    View View { get; }


    Properties Properties { get; set; }


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
    Size ArrangeViews(Size final);

    /// <summary>
    /// Render the control
    /// </summary>
    void Render(RenderContext context) { }
}

