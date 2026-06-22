# Coding agents / AI notes

This repo experiments with a minimal C# GUI stack (WebAssembly + native Windows) using a Model Data View approach.

## Design direction (MDV / MDCV)
	
Goal: replace MVVM-style runtime bindings with **generated, compile-time contracts**.

- **View (V)** declares the data it needs (currently in ZUI JSON `data`; other hosts may use an equivalent XAML `data` section).
- Code generation produces:
  - Code-behind partial controller class (`*.zui.json` → `<ViewName>.Control.g.cs`)
  - a strongly typed **data interface** (`*.zui.json` → `<ViewName>.Contract.g.cs`)
  - a concrete **data class** that implements it (`*.zui.json` → `<ViewName>.Data.g.cs`)
  - TBD: optionally, a **data-command** surface (MDCV) for typed interactions/commands
- You can:
  - use the generated data class as-is (e.g., deserialize JSON directly into it), or
  - extend it as a `partial` class to map to/from the domain **model (M)**.
- Avoid runtime reflection for MDV/data propagation and bindings. Prefer code generation (or other compile-time mechanisms) so updates are strongly-typed, AOT-friendly, and trimming-safe.

## Terminology

- **Model (M):** domain logic/types; should remain UI-agnostic when practical.
- **Data (D):** view-shaped data contract + implementation generated from the view’s declared needs.
- **Commands (C) (optional):** typed interaction surface generated alongside data.
- **View (V):** the renderer/layout/input layer that consumes the generated data interface.

## Editing guidelines

- Prefer minimal allocations and small payloads (WASM download size matters).
- Avoid adding new dependencies unless necessary.
- Generated code should not be hand-edited; change the source schema/generator instead.
- The source generator targets `netstandard2.0` (avoid language/runtime features that require newer TFMs inside `ZurfurGuiGen`).

## JSON coding standards (ZUI)

The `.zui.json` and `.zss.json` files use a relaxed JSON dialect parsed by `ZurfurGuiGen/Json.cs`. Two extensions
beyond standard JSON are supported:

- **`// line comments`** — a `//` outside a string value starts a comment that runs to the end of the line.
- **Trailing commas** — a comma after the last property in an object or the last element in an array is silently
  accepted. This matches the JSONC convention used by `tsconfig.json`, VS Code `settings.json`, etc.

These two relaxations are intentional. Strict JSON interoperability is not a goal because these files are consumed
exclusively by the generator. Comments and trailing commas reduce friction for both human and AI editors.

- In ZUI JSON, all type names are PascalCase (including built-in aliases like `Int`, `Bool`, `String`, etc.).
- In ZUI JSON, all field names are camelCase.
- The source generator converts from JSON style to C# style when generating code (field/property names become PascalCase in C#, and built-in type aliases are normalized to the corresponding C# keywords).
- Comments are collected by the generator and emitted into generated code as XML doc comments where appropriate.

Naming notes:

- The generated controller type name is exactly `<ViewName>` (it is not suffixed).
- For view code-behind, use a partial class in `<ViewName>.Control.cs`.
- If `.data` is present, the generator emits:
  - a data contract interface `I<ViewName>Data` in `<ViewName>.Contract.g.cs`
  - a data implementation class `<ViewName>Data` in `<ViewName>.Data.g.cs` (generated as `partial` if you provide `<ViewName>.Data.cs`)
- For data code-behind/extensibility, use a partial class in `<ViewName>.Data.cs`.
- All generated and user-authored partials for a view are in the same namespace (the namespace comes from the JSON `.namespace`).
- For generic controls (e.g. `ComboBox<Item>`), the same conventions apply with the type parameter appended:
  the controller is `ComboBox<Item>`, the data interface is `IComboBoxData<Item>`, and the data class is `ComboBoxData<Item>`.
- The `.implements` field names a constraint control whose data interface the current control's data interface extends.
  A control that declares `.implements: "ComboBoxItem"` will have `IComboBoxItemTextData : IComboBoxItemData` generated.
- `//` line comments immediately preceding a `.data` property or the top-level JSON object are captured by the generator
  and emitted as `<summary>` XML doc comments in all generated files (controller, data class, and data interface).

Detection rules (important for maintaining conventions):

- A view is identified by `*.zui.json` filename (e.g. `Button.zui.json` → `<ViewName>` is `Button`).
- Controller code-behind is detected **by filename**: `<ViewName>.Control.cs`.
- Data code-behind is detected **by filename**: `<ViewName>.Data.cs`.
- The generator also validates that the namespace declared in these `.cs` files matches the JSON `.namespace`.

## Code generation overview (`ZurfurGuiGen.GenerateZui`)

This repo uses a Roslyn incremental source generator (`ZurfurGuiGen/GenerateZui.cs`) to turn UI JSON files into C#.
It generates these outputs:

### 1) Per-view controller (`*.zui.json` → `<ViewName>.Control.g.cs`)

- Each `*.zui.json` file describes a view/control.
- The generator emits a controller class named after the file (for example, `MyView.zui.json` → `MyView.Control.g.cs`).
- The generated controller embeds the JSON, calls `Loader.Load(...)` to build the view tree, and exposes named child
  controls as fields (based on `.name` in the JSON).
- If you provide a matching hand-written `<ViewName>.Control.cs`, the generated class becomes `partial` so you can add
  code-behind without editing generated code.


### 2) Per-view data contract (`*.zui.json` → `<ViewName>.Contract.g.cs` + `<ViewName>.Data.g.cs`)

- If a view declares a `.data` section, the generator emits:
  - `I<ViewName>Data` in `<ViewName>.Contract.g.cs`
  - `<ViewName>Data : I<ViewName>Data` in `<ViewName>.Data.g.cs`
- If you provide a matching hand-written `<ViewName>.Data.cs`, the generated data class is emitted as `partial` so you can
  extend it.

#### `.data` bind keywords

Every entry in a `.data` section must declare a `"bind"` field. The valid values are:

| `bind` value | `PropertyKey` emitted | Data class field | Use when |
|---|---|---|---|
| `"styled"` | ✅ yes | ✅ yes | Value participates in style resolution (nullable → style fallback when `null`). Example: `text`, `foregroundColor`. |
| `"data"` | ❌ no | ✅ yes | Pure data — never styled. Example: `selectedIndex`, `isChecked`, `isExpanded`. Collections must use this. |
| `"_child.prop"` | ❌ no | ✅ yes (forwarded) | Binds the data property to a named child control's view property at runtime. |

- **`"styled"`** emits a static `PropertyKey<T>` on the controller (or its non-generic companion class for generic controls). When `DataContext` changes, `OnDataContextPropertyChanged` calls `View.SetProperty` / `View.RemoveProperty`. Nullable `"styled"` properties remove the property when set to `null`, allowing a style sheet to supply a fallback value.
- **`"data"`** stores the value only in the generated data class. No `PropertyKey` is emitted. `OnDataContextPropertyChanged` and `SyncAllPropertiesToView` skip these properties entirely — only code-behind observes them via `INotifyPropertyChanged`.
- Collections (`[]Type`) **must** use `"bind": "data"`. Using any other value is a generator error.

#### Collection bindings (`[]Type` syntax)

A `.data` entry whose `type` starts with `[]` declares a collection of item data objects rather than a single value.

```jsonc
".data": {
	"items": { "type": "[]ComboBoxItem", "bind": "data" }
}
```

Rules and generated output:

- `[]Type` is never nullable — `?[]Type` is a generator error. Use `[]Type` only.
- Collections must use `"bind": "data"` — any other value is a generator error.
- The generator derives the item interface name as `I<Type>Data` (same `I`-prefix convention as all data contracts).
- The contract property type is `ObservableCollection<I<Type>Data>` (fully qualified as
  `global::System.Collections.ObjectModel.ObservableCollection<I<Type>Data>`).
- The generated data class initializes the field with `new ObservableCollection<I<Type>Data>()` in its constructor,
  so callers always receive a live, non-null collection.
- The controller's `SyncAllPropertiesToView` and `OnDataContextPropertyChanged` skip collection properties — the
  control's code-behind is responsible for observing collection changes and updating the view.

Because the generator skips view-sync for collections, controls that use `[]Type` bindings must be implemented as
hand-written partial classes (`<ViewName>.Control.cs`) that subscribe to `CollectionChanged` or react to data-context
changes as needed. See `ComboBox.Control.cs` and `docs/ComboBox.md` for a worked example.

#### Data binding runtime behavior

- When `DataContext` is set, the controller subscribes to `INotifyPropertyChanged.PropertyChanged` events
- `"styled"` bindings: `OnDataContextPropertyChanged` calls `View.SetProperty` (or `View.RemoveProperty` for
  nullable properties set to `null`, allowing style fallback); `SyncAllPropertiesToView()` pushes all current values on assignment
- `"data"` bindings: stored only in the data object — `OnDataContextPropertyChanged` and `SyncAllPropertiesToView` skip them entirely; code-behind observes changes via `INotifyPropertyChanged` directly
- Data properties can also be set directly from `.zui.json` — unknown (non-`.`) property names without a dot are
  treated as data properties and applied via `Loader.ApplyDataProperties` after the control tree is built

### 3) Per-project registry (`*.zui.json` + `*.zss.json` → `ZurfurMain.g.cs`)

- The generator collects all views (`*.zui.json`) and stylesheets (`*.zss.json`) in the project.
- It emits a `static partial class ZurfurMain` with an `InitializeControls()` method that:
  - runs static constructors so control properties get registered
  - registers generated controls with `Loader.RegisterControl(...)`
  - registers styles with `Loader.RegisterStyleSheet(...)`

High-level runtime flow: your app's entry point calls the generated initialization, then creates a generated controller
(or loads one by name) to build and render the UI.

#### Note on file discovery: AdditionalFiles

For the generator to automatically process your `.zui.json`, `.zss.json`, or `.zth.json` files, they should be
included as **AdditionalFiles** in your project. This is controlled by the file's build action in Visual Studio
or by an `<ItemGroup>` in your `.csproj`:

```xml
<ItemGroup>
  <AdditionalFiles Include="**\*.zui.json" />
  <AdditionalFiles Include="**\*.zss.json" />
  <AdditionalFiles Include="**\*.zth.json" />
</ItemGroup>
```

If you do not set the build action to "C# analyzer" (or "AdditionalFiles"), the generator will not see the file.
Some project templates may add these rules automatically, but if your files are not being picked up, check the
build action or add the above ItemGroup to your project file.


## Generic controls

Controls can be made generic by using a type parameter and `where` constraint in the `.controller` field:

```jsonc
{ ".controller": "ComboBox<Item> where Item : ComboBoxItem" }
```

The type parameter (`Item`) and constraint (`ComboBoxItem`) are parsed by the generator. The constraint names
another control whose generated data interface becomes the C# constraint. So `where Item : ComboBoxItem` becomes
`where Item : IComboBoxItemData` in the generated C#:

```csharp
public sealed partial class ComboBox<Item> : Controllable
	where Item : IComboBoxItemData { ... }
```

The generic data interface and data class mirror the same parameter:

```csharp
public interface IComboBoxData<Item> : INotifyPropertyChanged
	where Item : IComboBoxItemData { ... }

public sealed class ComboBoxData<Item> : IComboBoxData<Item>
	where Item : IComboBoxItemData { ... }
```

When the collection binding type is the type parameter (`[]Item`), the generator resolves it to the constraint's
data interface (`ObservableCollection<IComboBoxItemData>`) rather than leaving it as the raw parameter, keeping
the data layer non-generic where possible.

### Constraint controls (`.controller` target)

The control named in the `where` clause (e.g. `ComboBoxItem`) acts purely as a **data shape contract**. It defines
the minimum set of `.data` properties that every concrete item must provide. It should not contain layout or visual
content — it exists only to establish the interface.

The constraint name should be specific to the container (e.g. `ComboBoxItem`, `TreeNode`) rather than a
generic root like `ControlItem`, even if the properties it declares today are universal (e.g. `isEnabled`,
`tag`). The reason is that the constraint name is also the **factory scope key**: `Loader.CreateDataController<IComboBoxItem>(itemData)`
uses `IComboBoxItem` to look up which registered concrete controller to instantiate for a given item. A
shared generic root would collapse all item types into one factory bucket and lose the ability to
discriminate between, say, combo box items and tree nodes. Each container therefore defines its own
constraint, and universal properties are simply inherited by every implementing control via `.implements`.

```jsonc
// ComboBoxItem.zui.json — defines the required data shape; no visual content
{
	".controller": "ComboBoxItem",
	".data": {
		"isEnabled": { "type": "bool",    "bind": "styled" },
		"tag":        { "type": "?object", "bind": "styled" }
	}
}
```

The generator does **not** emit a constraint interface (e.g. `IComboBoxItem`) for generic controls, because C#
does not allow open generic types as generic constraints.

### Implementing controls (`.implements`)

A concrete item control that should work inside a generic container declares `.implements` pointing to the
constraint control:

```jsonc
// ComboBoxItemText.zui.json — a concrete item that satisfies ComboBoxItem
{
	".controller": "ComboBoxItemText",
	".namespace":  "ZurfurGui.Controls",
	".implements": "ComboBoxItem",
	".classes":    [ "ComboBoxItemText" ],
	".data": {
		"text": { "type": "TextLines", "bind": "_itemText.text" }
	},
	".content": [ { ".name": "_itemText", ".controller": "TextView" } ]
}
```

Notice that `isEnabled` and `tag` (the properties declared by `ComboBoxItem`) are **not** repeated in the
`.data` section. The generator automatically inherits them from the constraint and includes them in the
generated controller, data class, and data interface. This means:

- Adding a property to the constraint control propagates to all implementing controls automatically, even
  those compiled into third-party assemblies referencing the library.
- The generated `IComboBoxItemTextData : IComboBoxItemData` interface already contains the inherited
  properties, so callers and style sheets never need to know whether a property is local or inherited.

#### Generated data class for implementing controls

When `.implements` is set, the generated concrete data class (e.g. `ComboBoxItemTextData`) uses **delegation**
rather than re-implementing inherited properties from scratch:

- A private `readonly` instance of the implemented data class is created and named
  `__implement{Implements}` (e.g. `__implementComboBoxItem`). The `__implement*` prefix makes it clear
  this is composition rather than C# inheritance, and the suffix names the specific contract — leaving
  room for multiple `.implements` entries in the future.
- In both constructors, `__implementComboBoxItem.PropertyChanged` is subscribed and forwarded onto the
  outer instance, so subscribers holding a reference to `ComboBoxItemTextData` receive `PropertyChanged`
  notifications for inherited properties (`IsEnabled`, `Tag`) as well as own properties (`Text`).
- Inherited properties are thin delegating wrappers — getter and setter both route through the implement instance:

```csharp
public bool IsEnabled
{
	get => __implementComboBoxItem.IsEnabled;
	set => __implementComboBoxItem.IsEnabled = value;
}
```

- Own properties (those declared in the implementing control's own `.data` section) retain full
  backing fields, `s_*EventArgs` statics, and equality-guarded setters as normal.
- The `s_*EventArgs` statics and backing fields for inherited properties are **not** emitted in the
  implementing class — they already exist inside `{Implements}Data` and are used there.

This approach avoids duplicating logic, keeps the generated code size proportional to the number of own
properties, and makes the `PropertyChanged` forwarding explicit and auditable.

The generator resolves inherited bindings using one of two strategies:

1. **Same project** — the constraint control's `DataBinding` list is looked up directly in the in-memory
   generator state. This is always used when the constraint and the implementing control are in the same
   `.csproj`.
2. **Referenced assembly** — when the constraint control lives in a referenced DLL, the generator queries
   the Roslyn `Compilation` to find the compiled `I{Implements}Data` interface and synthesizes `DataBinding`
   objects from its property members.

If the `.implements` target cannot be found by either strategy, the generator emits a **ZUI005** error.

If a property in the `.data` section has the same name as an inherited property, the generator emits a
**ZUI006** error and instructs the author to remove the duplicate.

The generator also emits a controller interface `IComboBoxItem` for the constraint control. Concrete item
controllers implement this interface by providing a `DataContext` property typed as their own specific data
interface. The generated code includes the necessary bridge so callers can treat any item controller as
`IComboBoxItem` without casting.

The implementing control is also registered in a data-controller lookup table keyed by its data interface
type. At runtime, `Loader.CreateDataController<IComboBoxItem>(itemData)` finds the right factory without
requiring the generic container to know concrete types.

### Cross-assembly `.implements` synthesis

When the constraint control lives in a referenced assembly, the generator synthesizes inherited bindings from
Roslyn metadata rather than from the original JSON source. The following table documents what is and is not
supported in that path — these are intentional design boundaries, not temporary bugs:

| Property kind | Same-project | Cross-assembly |
|---|---|---|
| Scalar `bool`, `string`, value types | ✅ | ✅ |
| Nullable reference types (`?object`) | ✅ | ✅ (via `NullableAnnotation`) |
| `"bind": "styled"` | ✅ | ✅ (hardcoded — all synthesized bindings use `"styled"`) |
| `ObservableCollection<>` (collection) | ✅ | ❌ ZUI007 error — must be declared explicitly in `.data` |
| XML doc comments | ✅ | ❌ comments are not preserved in compiled metadata |

The delegation approach in `ZuiEmitData.cs` means the generated **data class** for inherited properties never
inspects the `Bind` value — it simply delegates to the already-generated `{Implements}Data` instance. However,
the generated **controller class** still uses binding metadata (e.g. `Bind`, `IsCollection`) from `allBindings`
when emitting `OnDataContextPropertyChanged` and `SyncAllPropertiesToView`. For this reason, constraint controls
should only declare properties with `"bind": "styled"` and no `ObservableCollection` bindings.

### Property keys and generics

In C#, static fields on a generic class are per closed type — `ComboBox<A>` and `ComboBox<B>` each have their
own copy. This would cause duplicate `PropertyKey` registration exceptions at startup. To avoid this, the generator
emits `PropertyKey` fields into a separate **non-generic companion static class** with the same base name:

```csharp
// PropertyKeys for ComboBox<> live here rather than on the generic class
// to avoid per-closed-type static field duplication.
public static class ComboBox {
	public static readonly PropertyKey<int> SelectedIndex = new(..., typeof(ComboBox<>), ...);
}
```

Hand-written `PropertyKey` fields that belong conceptually to the control but must live outside the generic class
(e.g. `ScrimColor`) are declared directly in the hand-written `ComboBox.Control.cs` partial, using
`typeof(ComboBox<>)` as the owner type.

### Static initialization order

Static fields on generic classes are not initialized until the first time the closed type is used. Since
`PropertyKey` registration must happen before style sheets are loaded, the generated `ZurfurMain.g.cs` calls
`RuntimeHelpers.RunClassConstructor` for both the open generic type **and** each registered closed form:

```csharp
RuntimeHelpers.RunClassConstructor(typeof(ComboBox<>).TypeHandle);
RuntimeHelpers.RunClassConstructor(typeof(ComboBox<IComboBoxItemTextData>).TypeHandle);
```

## Two-tree architecture

MDV creates **two parallel, independent graphs**:

1. **Control tree** (`Controllable` → `View` → child `View` nodes)
   - Built depth-first by `Loader.Load()` deserializing ZUI JSON
   - Each control has a `View` with optional children, layout, and draw handlers
   - Hierarchy: parent `View` → `_children` list → child `View` instances

2. **DataContext tree** (strongly-typed data objects implementing generated interfaces)
   - Built after control tree initialization
   - Uses `INotifyPropertyChanged` for reactivity
   - **May** include references to sub-control `DataContext` objects, but only when explicitly declared by the view's `.data` section
	 (i.e., the data graph is *selectively composed* and is not required to mirror the control tree)

**Key principle:** Controls reference data; data may reference other data objects—but **data never references controls**.
The control tree and data graph are logically independent; the data graph shape is determined by what the view declares
in `.data`.

### Initialization sequence (per control)

Generated `InitializeControl()` runs in this order:

1. **Create View**: `View = new(this)` — attaches controller to view
2. **Build control tree**: `Loader.Load(this, _zuiJsonContent)`
   - Deserializes JSON properties
   - Recursively creates child controls via `Loader.CreateControl()` (which calls child's `InitializeControl()`)
   - Adds child views via `View.AddChild()`
3. **Cache named controls**: `_title = (TextView)View.FindByName("_title").Controller` — stores references to named children
4. **Create DataContext tree**: `DataContext = CreateDefaultDataContext()`
   - For primitive types: initializes with default values (e.g., `new TextLines()`)
   - For sub-control data (optional): if a `.data` binding targets a named control itself (e.g., `"bind": "card1"`), the
	 generated initializer uses the child's already-initialized `DataContext` (e.g., `Card1 = card1.DataContext`)
   - Otherwise, the initializer creates new view-shaped data objects (e.g., `Title = new TextLines()`)
5. **Apply JSON data properties**: `Loader.ApplyDataProperties(this)` — deserializes any data properties written
   directly in the `.zui.json` file (camelCase names without a leading `.`) and pushes them into `DataContext`
   via `SetDataProperty`. Children are processed before parents.

**Critical:** Child controls are fully initialized (including their `DataContext`, if any) before the parent's
`CreateDefaultDataContext()` runs, so parent data can safely reference child data when explicitly declared.

### MDV with generic controls

Generic controls follow the same MDV pattern as non-generic ones. The container (`ComboBox<Item>`) owns a
`DataContext` of type `IComboBoxData<Item>`, which exposes the item collection as
`ObservableCollection<IComboBoxItemData>`. The concrete item type is only known when the closed form is
instantiated (e.g. `ComboBox<IComboBoxItemTextData>`).

The hand-written code-behind (`ComboBox.Control.cs`) observes the collection and calls
`Loader.CreateDataController<IComboBoxItem>(itemData)` to instantiate the right item controller for each element —
the generic container never references the concrete item type directly. This keeps the container reusable with any
conforming item type, while the data model remains strongly typed end-to-end.

### Relation to MVVM

This is similar in spirit to MVVM having a visual/control tree plus a ViewModel object graph, but MDV does not assume
a 1:1 tree shape. Like MVVM, the data side should not reach into controls; integration should happen through declared
contracts/bindings.

## Where to look first (for AI agents)

- `ZurfurGuiGen/GenerateZui.cs`: source generator entry point; wires up ZUI and ZSS pipelines.
- `ZurfurGuiGen/ZuiInput.cs`: collects data from `.zui.json` / `.zss.json` files into `FileInfo`; parses generic `.controller` syntax, `where` constraints, `.implements`, and top-level `$comment` into metadata fields.
- `ZurfurGuiGen/ZuiSchema.cs`: parses `.data` bindings, named-control discovery, `$comment` injection per binding, and control-name-to-C#-type translation (including generic forms).
- `ZurfurGuiGen/ZuiEmitController.cs`: emits the controller class, `InitializeControl`, `DataContext` property,
  `OnDataContextPropertyChanged`, `SyncAllPropertiesToView`, and `SetDataProperty`; handles generic class headers and non-generic companion key containers.
- `ZurfurGuiGen/ZuiEmitContract.cs`: emits `I<ViewName>Data` interface; skips constraint-interface generation for generic controls; propagates top-level and per-binding XML doc comments.
- `ZurfurGuiGen/ZuiEmitData.cs`: emits `<ViewName>Data` implementation class; handles generic data classes and top-level doc comment propagation.
- `ZurfurGuiGen/ZuiEmitMain.cs`: emits `ZurfurMain.InitializeControls()` — control registration, style sheet registration, `RunClassConstructor` calls for both open generic types and each closed generic instantiation (to ensure property keys exist before style loading).
- `ZurfurGuiGen/ZuiEmit.cs`: shared code-emission helpers.
- `ZurfurGuiGen/Json.cs`: generator JSON parser (does not use `System.Text.Json`); supports `//` line comments, trailing commas, captures comments and injects them as `"$comment"` into adjacent dictionaries, and strips all `$`-prefixed keys during serialization so generator-only metadata is not embedded in generated `.cs` files.
- `ZurfurGui/Loader.cs`: runtime loader, `RegisterControl`, `RegisterStyleSheet`, `Load`, `ApplyDataProperties`; factory-based control registry keyed by data interface type; `CreateDataController<TConstraint>(itemData)` for generic item instantiation.
- `ZurfurGui/Styles/Style.cs`: style property resolution and caching (`GetStyle`, `FindStyle`,
  `EnumerateStyledValues`).
- `ZurfurGui/Controls/Panel.Control.cs`: all Panel `PropertyKey` definitions (attached properties).
- `ZurfurGui/Controls/*.zui.json`: view/control definitions and `.data` declarations.
- `docs/ComboBox.md`: how the ComboBox control works, how to use it, and how to create custom item renderers.

### Naming conventions for generic controls

| JSON name | C# name | Notes |
|---|---|---|
| `ComboBox<Item>` | `ComboBox<Item>` | Controller class |
| `ComboBox<Item>` | `IComboBoxData<Item>` | Data contract interface |
| `ComboBox<Item>` | `ComboBoxData<Item>` | Data implementation class |
| `ComboBoxItem` | `IComboBoxItemData` | Constraint data interface |
| `ComboBoxItem` | `IComboBoxItem` | Constraint controller interface (used in `CreateDataController<IComboBoxItem>`) |
| `ComboBoxItemText` | `IComboBoxItemTextData` | Implementing data interface (extends `IComboBoxItemData`) |
| `ComboBoxItemText` | `ComboBoxItemText` | Controller for concrete item |

Usage sites in JSON (e.g. a named child control) use the concrete form:

```jsonc
{ ".name": "_themeComboBox", ".controller": "ComboBox<ComboBoxItemText>" }
```

The generator translates this to the C# type `ComboBox<IComboBoxItemTextData>` in the generated code.


