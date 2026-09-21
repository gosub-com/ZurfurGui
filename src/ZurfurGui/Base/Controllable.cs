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
    /// Load content.  By default, it's a Panel and content gets loaded accordingly.
    /// Windows, Tabs, and other complex controls may need to override this function.
    /// The contents parameter contains the contents from the components parent.
    /// The View.Properties[Zui.Content] still contains the original content properties
    /// from this component's zui.json.
    /// TBD: From a user perspective we should probably copy the parent content to this 
    ///      component.  This components content is part of the visual tree, while the
    ///      parent content is part of the logical tree.  We will get that sorted out later.
    /// </summary>
    void LoadContent(Properties[]? contents, ControlCreationContext context) 
    { 
        if (contents != null)
            foreach (var property in contents)
                View.AddChild(Loader.CreateControl(property, context).View);
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

