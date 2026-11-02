using ZurfurGui.Base;

namespace ZurfurGui.Generated;

/// <summary>
/// The entry points called by code emitted by the ZUI source generator.  The generated code is
/// compiled into the application's assembly, so these must be public, but application code should
/// never call them.  This is the only generator-facing surface of the loader, which keeps
/// <see cref="Loader"/> free of generator hooks.  The generator and library must be built from the
/// same version.
/// </summary>
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public static class ZuiRuntime
{
    /// <summary>
    /// Loads the control's JSON content into the target.  Called from the generated
    /// InitializeControl function.
    /// </summary>
    public static void Load(ZuiControllable target, string json)
        => Loader.Load(target, json);

    /// <summary>
    /// Registers a control by name with its type and factory.  Called from the generated
    /// InitializeControls function.
    /// </summary>
    public static void RegisterControl(string name, Type type, Func<ZuiControllable> factory)
        => Loader.RegisterControl(name, type, factory);

    /// <summary>
    /// Registers the factory that creates an item controller for a generated data type.  Called
    /// from the generated InitializeControls function.
    /// </summary>
    public static void RegisterDataController(Type dataType, Func<object, ZuiControllable> factory)
        => Loader.RegisterDataController(dataType, factory);
}
