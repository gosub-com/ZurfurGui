namespace ZurfurGui.Base;

/// <summary>
/// The application-facing contract of a control.  Every control has a <see cref="View"/> and may
/// react to being attached to the visual tree.  The members used only by the ZUI source generator
/// live in <see cref="Generated.ZuiControllable"/>, which the generated code implements.
/// </summary>
public interface Controllable
{
    /// <summary>
    /// Sets a data property by name.  Returns true if the property was set successfully, false otherwise.
    /// </summary>
    bool SetDataProperty(string name, object? value) { return false; }

    /// <summary>
    /// The main control view.  Each control must have a MainView, that is readonly (i.e. never changes)
    /// </summary>
    View View { get; }

    /// <summary>
    /// The view that receives content supplied by the control's parent.
    /// By default, parent content is added to the control's main view.
    /// </summary>
    View ContentHost => View;

    /// <summary>
    /// Called after being attached to the visual tree
    /// </summary>
    void OnAttach() { }

    /// <summary>
    /// Called when the view is removed from a visual tree that is attached to an AppWindow.
    /// Runs while the view is still attached (its Parent and AppWindow are still valid), and
    /// children are notified before their parent.
    /// </summary>
    void OnDetach() { }

}
