using System.Collections.Generic;
using ZurfurGui.Property;
using static ZurfurGui.Loader;

namespace ZurfurGui.Base;

public interface Controllable
{
    /// <summary>
    /// Control type name, set by generated code
    /// </summary>
    string TypeName { get; }

    /// <summary>
    /// Control type namespace, set by generated code
    /// </summary>
    string TypeNamespace { get; }

    /// <summary>
    /// Control uses, set by generated code
    /// </summary>
    TextLines TypeUses { get; }

    /// <summary>
    /// Data property information for controls with data bindings.
    /// Returns an empty dictionary if the control has no data bindings.
    /// </summary>
    IReadOnlyDictionary<string, DataPropertyInfo> DataPropertyInfo { get; }

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
    /// Loads the control's own ZUI-defined visual children.
    ///
    /// This is separate from <see cref="LoadParentContent"/>, which receives content supplied by the parent.
    /// Controls with a separate internal visual structure may override this method.
    /// </summary>
    void LoadTemplateContent(Properties[]? contents, ControlCreationContext context)
    {
        if (contents != null)
            foreach (var property in contents)
                View.AddChild(Loader.CreateControl(property, context).View);
    }

    /// <summary>
    /// Loads content supplied by the control's parent.
    ///
    /// By default, content is added directly to the control's view. Controls with a dedicated content host,
    /// such as ScrollViewer, may override the content host to route parent content there.
    /// </summary>
    void LoadParentContent(Properties[]? contents, ControlCreationContext context)
    { 
        if (contents != null)
            foreach (var property in contents)
                ContentHost.AddChild(Loader.CreateControl(property, context).View);
    }

    /// <summary>
    /// Called after being attached to the visual tree
    /// </summary>
    void OnAttach() { }

    /// <summary>
    /// Called before being detached from the visual tree.  TBD: Not actually called yet
    /// </summary>
    void OnDetach() { }

}

