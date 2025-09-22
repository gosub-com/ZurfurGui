using ZurfurGui.Layout;
using ZurfurGui.Draw;
using ZurfurGui.Base;

namespace ZurfurGui.Controls;

public interface Controllable
{
    /// <summary>
    /// Unique type name of control, which should be set by generated code
    /// </summary>
    string TypeName { get; }

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
    void LoadContent(Properties[]? contents) 
    { 
        if (contents != null)
            foreach (var property in contents)
                View.AddChild(Loader.CreateControl(property).View);
    }

}

