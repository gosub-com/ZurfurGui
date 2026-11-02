using System.Collections.Generic;
using ZurfurGui.Base;
using ZurfurGui.Property;

namespace ZurfurGui.Generated;

/// <summary>
/// Identifies the control that is being loaded, so the loader can resolve the names of the
/// child controls inside it.  Created and used only by the loader.
/// </summary>
internal readonly record struct ControlCreationContext(
    string TypeName,
    string TypeNamespace,
    TextLines TypeUses)
{
    /// <summary>
    /// Creates the context for the given control.
    /// </summary>
    internal static ControlCreationContext From(ZuiControllable control)
        => new(control.TypeName, control.TypeNamespace, control.TypeUses);
}

/// <summary>
/// The part of the controller contract that is implemented only by code emitted by the ZUI source
/// generator.  Application code should use <see cref="Controllable"/> and never implement or call
/// this interface.  Members here may change whenever the generator changes, and the generator and
/// library must always be built from the same version.
/// </summary>
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public interface ZuiControllable : Controllable
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
    IReadOnlyDictionary<string, ZuiPropertyInfo> ZuiPropertyInfo { get; }
}
