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

    /// <summary>
    /// List of child controls, representing the DOM.  This tree can be different than the view tree.
    /// </summary>
    IList<Controllable> Controls { get; init; }


    /// <summary>
    /// Called only by View to setup the view tree.  This will often populate the main view with the 
    /// `View` in `Controls`, but may also add other views when the view tree is different from 
    /// the control tree. This does not recursively walk down the tree.
    /// This should only be called from View.BuildViewTree (never call from any other code).
    /// TBD: Move to private ControllableImpl so only View can call.
    /// </summary>
    void PopulateView();

    /// <summary>
    /// Returns the desired size of the control given the available screen size.
    /// Similar to MeasureOverride in WPF
    /// </summary>
    Size Measure(Size available);

    /// <summary>
    /// Arrange the controls children.
    /// Similar to ArrangeOverride in WPF
    /// </summary>
    Size ArrangeChildren(Size final);

}

