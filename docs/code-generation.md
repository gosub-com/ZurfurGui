# Code Generation

This repo uses a Roslyn incremental source generator (`src/ZurfurGuiGen/GenerateZui.cs`) to turn
ZUI and ZTH files into C#. The generator targets `netstandard2.0`, and the code it generates targets
`.NET 10`. For the file syntax, see [ZUI and ZTH file format](zui-format.md). For the `$data`
section and binding categories, see [Property binding](property-binding.md). For how the generated
registration code is used at startup, see [Initialization](initialization.md).

## File discovery

The generator processes `.zui` and `.zth` files included as project `AdditionalFiles`. In Visual
Studio, set the build action to `C# analyzer` / `AdditionalFiles`, or add the files in the project:

```xml
<ItemGroup>
	<AdditionalFiles Include="**\*.zui" />
	<AdditionalFiles Include="**\*.zth" />
</ItemGroup>
```

The project must also reference the generator as an analyzer:

```xml
<ProjectReference Include="..\ZurfurGuiGen\ZurfurGuiGen.csproj"
				  OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
```

If the generator does not pick up a file, check its build action or the project's `AdditionalFiles`
items. Generated files are placed under the build-generated source directory (for example,
`obj/Debug/net10.0/generated/ZurfurGuiGen/ZurfurGuiGen.GenerateZui/`) and must not be edited by hand.

## Naming and detection rules

- A view is identified by a `*.zui` filename (for example, `Button.zui` → `<ViewName>` is `Button`).
- The generated controller type name is exactly `<ViewName>` (it is not suffixed).
- Controller code-behind is detected **by filename**: `<ViewName>.Control.cs`.
- Data code-behind is detected **by filename**: `<ViewName>.Data.cs`.
- All generated and user-authored partials for a view are in the same namespace, which comes from
  the JSON `$namespace`. The generator validates that the namespace declared in the code-behind
  `.cs` files matches the JSON `$namespace` and reports an error if it does not.
- If `$data` is present, the generator emits a data class `<ViewName>Data`.
- For generic controls (for example, `ComboBox<Item>`), the same conventions apply with the type
  parameter appended: the controller is `ComboBox<Item>`, and the data class is
  `ComboBoxData<Item>`.
- A theme is identified by a `*.zth` filename, and its top-level `name` must match the filename.

## Outputs

### 1) Per-view controller (`*.zui` → `<ViewName>.Control.g.cs`)

- Each `*.zui` file describes a view/control.
- The generator emits a sealed controller class named after the file (for example,
  `MyView.zui` → `MyView.Control.g.cs`).
- The generated controller embeds the JSON, calls `Loader.Load(...)` to build the view tree, and
  exposes named child controls as fields (based on `$name` in the JSON).
- If you provide a matching hand-written `<ViewName>.Control.cs`, the generated class becomes
  `partial` so you can add code-behind without editing generated code.
- When the view has bindings, the controller also contains the strongly typed `DataContext`
  property, the `Properties` accessor, `OnDataContextPropertyChanged`, `SyncAllPropertiesToView`,
  `CreateDefaultDataContext`, and `SetDataProperty`. Their behavior is described in
  [Property binding](property-binding.md).

### 2) Per-view data class (`*.zui` → `<ViewName>.Data.g.cs`)

- If a view declares a `$data` section, the generator emits the concrete, sealed `<ViewName>Data`
  class implementing `INotifyPropertyChanged`.
- If you provide a matching hand-written `<ViewName>.Data.cs`, the generated data class is emitted
  as `partial` so you can extend it.
- If the view declares `$implements`, the generated data class implements that interface.
- There are no generated `I<ViewName>Data` contracts.

### 3) Per-project registry (`*.zui` + `*.zth` → `ZurfurMain.g.cs`)

The generator collects all views (`*.zui`) and themes (`*.zth`) in the project. It emits a
`static partial class ZurfurMain` with an `InitializeControls()` method that:

- runs static constructors for control property registration, including open and discovered closed
  generic controls, and the non-generic companion class of each generic control;
- registers ordinary controls with `Loader.RegisterControl(...)` using their namespace/name string;
- registers each concrete data-bearing control with `Loader.RegisterDataController(...)`, mapping its
  generated data type to a factory that assigns the same data instance to the new controller's
  `DataContext`;
- registers closed generic control usages discovered in ZUI files; and
- registers themes with `ThemeManager.RegisterTheme(...)`.

The project must contain a hand-written `static partial class ZurfurMain` with a `MainApp()` method
that calls `InitializeControls()`. If the project contains generated controls or themes but no
`ZurfurMain` class, the generator reports error `ZUI005`. The library itself provides this class
in `src/ZurfurGui/ZurfurMain.cs`.

## Generic controls

Controls can be made generic by using a type parameter and `where` constraint in the `$controller`
field:

```json5
{ $controller: "ComboBox<Item> where Item : IComboBoxItem" }
```

The generator accepts exactly one type parameter and one constraint. The constraint is a normal
handwritten C# type, not a generated data contract. The generated controller uses it directly:

```csharp
public sealed partial class ComboBox<Item> : Controllable
	where Item : IComboBoxItem { ... }
```

The generated data class mirrors the concrete type parameter:

```csharp
public sealed class ComboBoxData<Item> : INotifyPropertyChanged
	where Item : IComboBoxItem
{
	ObservableCollection<Item> Items { get; set; }
	int? SelectedIndex { get; set; }
}
```

When a collection binding type is the type parameter (`[]Item`), the generator preserves `Item`,
producing a strongly typed collection such as `ObservableCollection<Item>`.

### MDV with generic controls

Generic controls follow the same MDV pattern as non-generic ones. The container (`ComboBox<Item>`)
owns a `DataContext` of type `ComboBoxData<Item>`, which exposes the item collection as
`ObservableCollection<Item>`. The concrete item type is known in each closed form, such as
`ComboBox<ComboBoxItemTextData>`.

The hand-written code-behind (`ComboBox.Control.cs`) calls `Loader.CreateDataController(itemData)`
to instantiate the right item controller for each element. The generic container remains reusable
because the lookup is keyed by the runtime concrete data type, while the generated item data and
controller remain strongly typed end-to-end.

### Constraint types

A generic constraint should be a real C# type available to the generated controller. For ComboBox,
the library provides the minimal handwritten marker `ZurfurGui.Controls.IComboBoxItem`. An item view
normally supplies the marker with `$implements: "IComboBoxItem"` in its ZUI definition, causing the
generated data class to implement it.

The marker does not define generated data properties or create inheritance between item data
classes. Each item view declares its own `$data` properties.

### Concrete item controls

A concrete item control declares its own `$data` section, and its generated `<ViewName>Data` class
is a concrete type. If it is a ComboBox item, its ZUI definition also declares
`$implements: "IComboBoxItem"`.

The generator emits a concrete data-controller registration for each non-generic data-bearing
control. For example, `ComboBoxItemTextData` is registered with `ComboBoxItemText`. At runtime,
`Loader.CreateDataController(itemData)` looks up the factory by the item's runtime concrete data
type and returns a controller whose `DataContext` is already assigned.

This design avoids generated data interfaces, interface-based factory lookup, inherited `$data`
bindings, and cross-assembly `$implements` synthesis. Shared behavior should be expressed through
handwritten C# types or explicit composition rather than generator metadata.

### Property keys and generics

In C#, static fields on a generic class are per closed type — `ComboBox<A>` and `ComboBox<B>` each
have their own copy. This would cause duplicate `PropertyKey` registration exceptions at startup.
To avoid this, the generator emits an empty non-generic companion static partial class with the same
base name for every generic control. Any generated `PropertyKey` fields are emitted into that
companion rather than the generic class. Key fields are named after the binding with a `Property`
suffix:

```csharp
// PropertyKeys for ComboBox<> live here rather than on the generic class
// to avoid per-closed-type static field duplication.
public static partial class ComboBox {
	public static readonly PropertyKey<int> ExampleProperty = new(..., typeof(ComboBox<>), ...);
}
```

The companion is generated even when the generic control has no generated property keys.
Hand-written static members can be added through a matching partial companion when a generic control
needs them. The generated `InitializeControls()` runs the static constructor of both the open
generic type and its companion so keys are registered before style sheets are parsed.

## Diagnostics

The generator reports these diagnostics:

| Id | Meaning |
|---|---|
| `ZUI001` | A JSON parsing error, reported with the line and column. |
| `ZUI002` | An error while collecting a file, such as a missing key, a bad `bind` value, or a namespace mismatch. |
| `ZUI003` | An internal error during controller generation. |
| `ZUI004` | An exception while emitting the controller or data class. |
| `ZUI005` | The project has generated code but no `ZurfurMain` class. |

## Source map

- `ZurfurGuiGen/GenerateZui.cs`: source generator entry point; wires up ZUI and ZTH pipelines.
- `ZurfurGuiGen/ZuiInput.cs`: collects data from `.zui` / `.zth` files into `ZuiFileInfo`; parses
  generic `$controller` syntax, `where` constraints, and top-level `#comment` into metadata fields.
  Validates code-behind namespaces.
- `ZurfurGuiGen/ZuiSchema.cs`: parses `$data` bindings, named-control discovery, `#comment`
  injection per binding, and control-name-to-C#-type translation (including generic forms).
- `ZurfurGuiGen/ZuiEmitController.cs`: emits the controller class, `InitializeControl`,
  `DataContext` property, `PropertiesAccessor`, `OnDataContextPropertyChanged`,
  `SyncAllPropertiesToView`, and `SetDataProperty`; handles generic class headers and always emits
  non-generic companion classes for generic controls.
- `ZurfurGuiGen/ZuiEmitData.cs`: emits `<ViewName>Data` concrete data classes; handles generic data
  classes and top-level doc comment propagation.
- `ZurfurGuiGen/ZuiEmitMain.cs`: emits `ZurfurMain.InitializeControls()` — control registration,
  concrete data-controller factory registration, theme registration, and `RunClassConstructor`
  calls for open generic types and each closed generic instantiation.
- `ZurfurGuiGen/ZuiEmit.cs`: shared code-emission helpers.
- `ZurfurGuiGen/ZuiDiagnostic.cs`: diagnostic creation.
- `ZurfurGuiGen/Json.cs`: generator JSON parser (does not use `System.Text.Json`); captures
  comments and injects them as `#comment` into adjacent dictionaries, and uses explicit
  `RemoveKeys` calls before generator-only metadata is embedded in generated `.cs` files.
