# Coding agents / AI notes

This repo experiments with a minimal C# GUI stack (WebAssembly + native Windows) using
a **Model Data View (MDV)** approach.

## Design direction (MDV)

Goal: replace **MVVM** style runtime bindings with **generated, compile-time view data shapes**.

- The **View (V)** declares the **data (D)** it needs (in ZUI JSON `data` section).
- Code generation produces:
  - Code-behind partial controller class (`*.zui` → `<ViewName>.Control.g.cs`)
  - A strongly typed data class (`*.zui` → `<ViewName>.Data.g.cs`)
  - Library initialization `ZurfurMain.cs` partial class, constructed from `*.zui` and `*.zth` files.
- You can:
  - use the generated data class as-is (for example, deserialize or populate it directly), or
  - extend it as a `partial` class to map to/from the domain **model (M)**.
- Avoid runtime reflection for MDV/data propagation and bindings. Prefer code generation (or other
  compile-time mechanisms) so updates are strongly typed, AOT-friendly, and trimming-safe.

## Terminology

- **Model (M):** domain logic/types; should remain UI-agnostic when practical.
- **Data (D):** view-shaped data generated from the view's declared needs.
- **View (V):** the renderer/layout/input layer that consumes the generated data class.

## Two-tree architecture

MDV creates **two parallel, independent graphs**:

1. **Control tree** (`Controllable` → `View` → child `View` nodes)
   - Built depth-first by `Loader.Load()` deserializing ZUI JSON
   - Each control has a `View` with optional children, layout, and draw handlers
   - Hierarchy: parent `View` → `_children` list → child `View` instances

2. **DataContext tree** (strongly typed concrete data objects implementing `INotifyPropertyChanged`)
   - Built after control tree initialization
   - Uses `INotifyPropertyChanged` for reactivity
   - May include references to sub-control `DataContext` objects, but only when explicitly declared by the view's
     `$data` section

**Key principle:** Controls reference data; data may reference other data objects—but **data never
references controls**. The control tree and data graph are independent; the data graph shape is
determined by what the view declares in `$data`.

### Relation to MVVM

This resembles MVVM's visual tree plus ViewModel graph, but MDV does not assume a 1:1 tree shape. The
data side should not reach into controls; integration should happen through declared bindings.

## ZUI and ZTH configuration syntax

`.zui` files are Zurfur UI configuration files, and `.zth` files are Zurfur Theme configuration files.
Both use a small modified JSON5 format parsed by `ZurfurGuiGen/Json.cs`. They are essentially JSON
with comments and dictionary keys do not need quotes. In addition to standard JSON, the parser
supports these JSON5 features:

- **`// line comments`** — a `//` outside a string value starts a comment that runs to the end of the line.
- **Trailing commas** — a comma after the last property in an object or the last element in an array is
  silently accepted. This matches the JSONC convention used by `tsconfig.json`, VS Code `settings.json`,
  etc.
- **Unquoted dictionary keys** — valid keys, including keys beginning with `$` and containing `.` or `-`,
  do not need quotes.
- Keys beginning with `$` are global symbols reserved by ZurfurGUI, such as `$controller`, `$name`, `$margin`, and
  `$width`.
- Type names are PascalCase (including built-in aliases like `Int`, `Bool`, `String`, etc.).
- Field names are camelCase.
- The source generator converts from JSON style to C# style when generating code (field/property names
  become PascalCase in C#, and built-in type aliases are normalized to the corresponding C# keywords).
- Comments are collected by the generator and emitted into generated code as XML doc comments where appropriate.

Naming notes:

- The generated controller type name is exactly `<ViewName>` (it is not suffixed).
- For view code-behind, use a partial class in `<ViewName>.Control.cs`.
- If `$data` is present, the generator emits a data class `<ViewName>Data` in
  `<ViewName>.Data.g.cs` (generated as `partial` if you provide `<ViewName>.Data.cs`).
- For data code-behind/extensibility, use a partial class in `<ViewName>.Data.cs`.
- All generated and user-authored partials for a view are in the same namespace (the namespace comes
  from the JSON `$namespace`).
- For generic controls (for example, `ComboBox<Item>`), the same conventions apply with the type
  parameter appended: the controller is `ComboBox<Item>`, and the data class is `ComboBoxData<Item>`.
- `ComboBox<Item>` uses the handwritten empty `IComboBoxItem` marker as its generic constraint. An item view declares
  that its generated data class implements the marker with `$implements: "IComboBoxItem"`.
- `//` line comments immediately preceding a `$data` property or the top-level JSON object are
  captured by the generator and emitted as `<summary>` XML doc comments in generated files (controller
  and data class).

Detection rules (important for maintaining conventions):

- A view is identified by a `*.zui` filename (for example, `Button.zui` → `<ViewName>` is `Button`).
- Controller code-behind is detected **by filename**: `<ViewName>.Control.cs`.
- Data code-behind is detected **by filename**: `<ViewName>.Data.cs`.
- The generator also validates that the namespace declared in these `.cs` files matches the JSON
  `$namespace`.

## Code generation overview (`ZurfurGuiGen.GenerateZui`)

This repo uses a Roslyn incremental source generator (`ZurfurGuiGen/GenerateZui.cs`) to turn UI JSON
files into C#.
It generates these outputs:

### 1) Per-view controller (`*.zui` → `<ViewName>.Control.g.cs`)

- Each `*.zui` file describes a view/control.
- The generator emits a controller class named after the file (for example,
  `MyView.zui` → `MyView.Control.g.cs`).
- The generated controller embeds the JSON, calls `Loader.Load(...)` to build the view tree, and
  exposes named child controls as fields (based on `$name` in the JSON).
- If you provide a matching hand-written `<ViewName>.Control.cs`, the generated class becomes `partial` so you can add
  code-behind without editing generated code.


### 2) Per-view data class (`*.zui` → `<ViewName>.Data.g.cs`)

- If a view declares a `$data` section, the generator emits the concrete `<ViewName>Data` class.
- If you provide a matching hand-written `<ViewName>.Data.cs`, the generated data class is emitted as
  `partial` so you can extend it.

#### `$data` bind keywords

Every entry in a `$data` section must declare a `"bind"` field. The valid values are:

| `bind` value | `PropertyKey` emitted | data property | Use when |
|---|---|---|---|
| `"data"` | No | Yes | State or data-only values. Collections must use this. |
| `"styledData"` | Yes | Yes | A nullable or non-nullable data value that can override a style or theme value. |
| `"styled"` | Yes | No | A style-only value configured by JSON, themes, or imperative view code. |
| `"property"` | Yes | No | A view-only value read with `GetProperty`, not theme lookup. |
| `"attachedProperty"` | Yes | No | An attached property binding, currently using the local view-property path. |
| `"attachedStyle"` | Yes | No | A style-aware attached binding, currently using the local view-property path. |
| `"data:<target>"` | No | Yes | Forwards a generated data property to a named child control's data property. |

**NOTE:** `View.GetStyle` is now private.  Always use `Control.Properties` to read a property or style
when the property is not attached and the control is known at compile time.  Use `View.GetProperty` to
read a property or style when the property is attached or the control is not known at compile time.  This
documentation will be updated to reflect this note in the future.

- **`"data"`** stores the value only in the concrete generated data class. The generated controller does
  not push the value to the view. Code-behind observes changes through `INotifyPropertyChanged`; use
  `DataContext` to read or write it.
- **`"styledData"`** emits a static `PropertyKey<T>` on the controller (or its non-generic companion
  class for generic controls). `Properties` reads the effective value with `GetStyle` and is read-only
  for this binding. Always use `Properties` to read a `styledData` value; write it through `DataContext`.
  When `DataContext` changes, `OnDataContextPropertyChanged` calls `View.SetProperty` or
  `View.RemoveProperty`. Setting nullable `styledData` to `null` removes the local override, allowing
  style or theme resolution to supply a fallback value.
- **`"styled"`** emits a `PropertyKey<T>` but no data property. It is configured through styles or
  imperative `View.SetProperty` calls. Its `Properties` accessor reads with `GetStyle` and writes with
  `View.SetProperty`.
- **`"property"`** emits a `PropertyKey<T>` but no data property, like `"styled"`. It should be read
  with `GetProperty`, which reads the view's property or key default without resolving theme values. Its
  `Properties` accessor writes with `View.SetProperty`.
- **`"attachedProperty"`** emits a `PropertyKey<T>` and `ZuiPropertyType.AttachedProperty`, with no
  data property. It currently uses the same local property access path as `"property"`;
  owner-independent application is deferred.
- **`"attachedStyle"`** emits a `PropertyKey<T>` and `ZuiPropertyType.AttachedStyle`, with no data
  property. It currently uses the same local style access path as `"styled"`; attached style resolution
  is deferred.
- **`"data:<target>"`** creates a parent data property and forwards its value to a property on the named
  child's `DataContext`, such as `"data:_child.text"`. Use the parent `DataContext` to read or write the
  forwarded value. The target may be a child `data` or `styledData` property, but not a child `styled`
  property. A `styledData` child target mirrors the value to its own view property. The `data` prefix is
  required; other prefixes and empty targets are errors.
- Without a colon, only `"data"`, `"styledData"`, `"styled"`, `"property"`, `"attachedProperty"`,
  and `"attachedStyle"` are valid bind values. Unknown values are generator errors rather than implicit
  forwarded bindings.
- Collections (`[]Type`) must use `bind: "data"`. Using any other value is a generator error.

`DataContext` contains `data`, `styledData`, and forwarded values. Use it for application data and for
writing `styledData`. The generated `Properties` accessor is view-focused and retains a `View`: it
exposes `styledData`, `styled`, and `attachedStyle` through `GetStyle`, and `property` and
`attachedProperty` through `GetProperty`. Data and forwarded bindings are not members of `Properties`.
Every binding has generated `ZuiPropertyInfo`; `styledData`, `styled`, `property`, `attachedProperty`,
and `attachedStyle` bindings have a `PropertyKeyId`, because data and forwarded values are applied
through `DataContext`. Attached kinds are recorded in metadata, but owner-independent lookup and
application are not implemented yet. Nullable `styledData` writes of `null` remove the local view
override so the style or theme fallback is visible when read later through `Properties`.

#### Collection bindings (`[]Type` syntax)

A `$data` entry whose `type` starts with `[]` declares a collection of data objects rather than a single value.

```json5
$data: {
    items: { type: "[]ComboBoxItem", bind: "data" }
}
```

Rules and generated output:

- `[]Type` is never nullable; `?[]Type` is a generator error. Use `[]Type` only.
- Collections must use `bind: "data"`; any other value is a generator error.
- For a regular item type, the generated property is an `ObservableCollection<TypeData>`.
- For a generic type parameter such as `[]Item` in `ComboBox<Item>`, the generated property remains
  `ObservableCollection<Item>`.
- The concrete data class initializes the collection in its constructor, so callers receive a live, non-null
  collection.
- The generated controller skips collection synchronization. A control that needs collection-driven updates must manage
  them in handwritten code-behind.

Because the generator skips view-sync for collections, controls that use `[]Type` bindings must implement any
collection synchronization they require in handwritten code-behind. Such controls may subscribe to
  `CollectionChanged` or react to data-context changes as needed. See `ComboBox.Control.cs` and `docs/combo-box.md` for
the current behavior.

#### Data binding runtime behavior

- When `DataContext` is set, the generated controller subscribes to `INotifyPropertyChanged.PropertyChanged` events.
- `"styledData"` bindings call `View.SetProperty` or `View.RemoveProperty`; `SyncAllPropertiesToView()` pushes current
  values when a data context is assigned.
- `"data"` bindings remain in the concrete data object. The generated controller does not push them to the view;
  code-behind observes `PropertyChanged` directly.
- `"data:<target>"` forwarded bindings assign the target child data property when the parent data property changes.
- The loader continues to switch on `ZuiPropertyType`: `data`, `styledData`, and forwarded values go through generated
  `SetDataProperty`; `styled`, `property`, `attachedStyle`, and `attachedProperty` values are applied to the view by
  `PropertyKeyId`. Attached kinds currently follow the same local property path as their non-attached counterparts.
  The generated
  `SetDataProperty` validates values and routes data-backed assignments through `Properties`. Child data properties are
  applied recursively before the parent's properties; generated initialization creates the parent's `DataContext` after
  `Loader.Load` returns.

### 3) Per-project registry (`*.zui` + `*.zth` → `ZurfurMain.g.cs`)

- The generator collects all views (`*.zui`) and themes (`*.zth`) in the project.
- It emits a `static partial class ZurfurMain` with an `InitializeControls()` method that:
  - runs static constructors for control property registration, including open and discovered closed generic
    controls
  - registers ordinary controls with `Loader.RegisterControl(...)` using their namespace/name string
  - registers each concrete data-bearing control with `Loader.RegisterDataController(...)`, mapping its generated data
    type to a factory that assigns the same data instance to the new controller's `DataContext`
  - registers closed generic control usages discovered in ZUI files
  - registers themes with `ThemeManager.RegisterTheme(...)`

High-level runtime flow: the app calls generated initialization before creating controls.
A generated controller loads its embedded ZUI JSON, creates child controls, creates its concrete `DataContext`, and
applies JSON data properties.

## Generic controls

Controls can be made generic by using a type parameter and `where` constraint in the `$controller` field:

```json5
{ $controller: "ComboBox<Item> where Item : IComboBoxItem" }
```

The type parameter (`Item`) and constraint (`IComboBoxItem`) are parsed by the generator. The constraint is a normal
handwritten C# type, not a generated data contract. The generated controller uses the same marker directly:

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

When a collection binding type is the type parameter (`[]Item`), the generator preserves `Item`, producing a strongly
typed collection such as `ObservableCollection<Item>`.

### MDV with generic controls

Generic controls follow the same MDV pattern as non-generic ones. The container (`ComboBox<Item>`) owns a
`DataContext` of type `ComboBoxData<Item>`, which exposes the item collection as `ObservableCollection<Item>`. The
concrete item type is known in each closed form, such as `ComboBox<ComboBoxItemTextData>`.

The hand-written code-behind (`ComboBox.Control.cs`) calls `Loader.CreateDataController(itemData)` to instantiate the
right item controller for each element. The generic container remains reusable because the lookup is keyed by the
runtime concrete data type, while the generated item data and controller remain strongly typed end-to-end.


### Constraint types

A generic constraint should be a real C# type available to the generated controller. For ComboBox, the library
provides the minimal handwritten marker `ZurfurGui.Controls.IComboBoxItem`. An item view normally supplies the marker
with `$implements: "IComboBoxItem"` in its ZUI definition, causing the generated data class to implement it.

The marker does not define generated data properties or create inheritance between item data classes.
Each item view declares its own `$data` properties.

### Concrete item controls

A concrete item control declares its own `$data` section, and its generated `<ViewName>Data` class is a concrete type.
If it is a ComboBox item, its ZUI definition also declares `$implements: "IComboBoxItem"`.

The generator emits a concrete data-controller registration for each non-generic data-bearing control. For example,
`ComboBoxItemTextData` is registered with `ComboBoxItemText`. At runtime, `Loader.CreateDataController(itemData)`
looks up the factory by the item's runtime concrete data type and returns a controller whose `DataContext` is already
assigned.

This design avoids generated data interfaces, interface-based factory lookup, inherited `$data` bindings, and
cross-assembly `$implements` synthesis. Shared behavior should be expressed through handwritten C# types or explicit
composition rather than generator metadata.

### Property keys and generics

In C#, static fields on a generic class are per closed type — `ComboBox<A>` and `ComboBox<B>` each have their own copy.
This would cause duplicate `PropertyKey` registration exceptions at startup. To avoid this, the generator emits
an empty non-generic companion static partial class with the same base name for every generic control. Any generated
`PropertyKey` fields are emitted into that companion rather than the generic class:

```csharp
// PropertyKeys for ComboBox<> live here rather than on the generic class
// to avoid per-closed-type static field duplication.
public static partial class ComboBox {
    public static readonly PropertyKey<int> SelectedIndex = new(..., typeof(ComboBox<>), ...);
}
```

The companion is generated even when the generic control has no generated property keys. Hand-written static members
can be added through a matching partial companion when a generic control needs them.

## Loader and runtime initialization

### Initialization sequence (per control)

Generated `InitializeControl()` runs in this order:

1. **Create View**: `View = new(this)` — attaches controller to view
2. **Build control tree**: `Loader.Load(this, _zuiJsonContent)`
   - Deserializes the control's own ZUI definition and merges its properties and layout
   - Recursively creates child controls via `Loader.CreateControl()` (which calls child's `InitializeControl()`)
   - Loads template content and applies JSON data properties recursively, children before parents
3. **Cache named controls**: `_title = (TextView)View.FindByName("_title").Controller` — stores references to named children
4. **Create DataContext**: `DataContext = CreateDefaultDataContext()`
   - For primitive types: initializes with default values (e.g., `new TextLines()`)
   - For sub-control data (optional): if a `$data` binding targets a named control itself (e.g., `bind: "card1"`), the
     generated initializer uses the child's already-initialized `DataContext` (e.g., `Card1 = card1.DataContext`)
    - For forwarded scalar bindings, initializes from the target child data property to preserve template values
    - Otherwise, the initializer creates new view-shaped data objects (e.g., `Title = new TextLines()`)

**Critical:** Child controls are fully initialized (including their `DataContext`, if any) before the parent's
`CreateDefaultDataContext()` runs, so parent data can safely reference child data when explicitly declared.

### Loader registration and lookup

`Loader` maintains separate registries for controls and for controllers that render concrete data objects:

- `RegisterControl` maps a controller name to a `Controllable` type and a parameterless factory. It is used when
  `CreateControl` reads a `$controller` value from ZUI JSON.
- `RegisterDataController` maps a concrete generated data `Type` to a factory that receives the data object and
  returns its item controller. `GetDataControllerFactory` performs an exact runtime-type lookup in this registry.

The control registry is name-based because ZUI contains controller names. Lookup tries the fully qualified name,
the containing control's namespace, its `$use` namespaces, and finally the built-in `ZurfurGui.Controls` namespace.
`RegisterControl` rejects a conflicting name/type registration but allows the same type to be registered again.

The data-controller registry is type-based rather than name-based. This preserves the concrete data instance and
avoids requiring generated interface contracts or metadata discovery. A control such as `ComboBox<Item>` calls
`Loader.CreateDataController(itemData)` when it needs to render an item. The loader uses `itemData.GetType()` to
find the factory, and the factory assigns that same object to the new controller's `DataContext`.

Registration happens before control creation. `Loader.Init` calls the application's `ZurfurMain.MainApp`, which must
call the generated `InitializeControls` method. That method runs relevant static constructors, registers ordinary
and closed-generic controls, registers concrete data-controller factories, and registers themes. Later, a generated
controller's `InitializeControl` calls `Loader.Load`; while loading its content, `CreateControl` resolves child
controllers through the control registry. Data contexts and JSON data properties are applied after the control tree
is built. Item data-controller lookup occurs afterward when a data-driven control, such as ComboBox, creates an item.

### File discovery: AdditionalFiles

For the generator to automatically process your `.zui` or `.zth` files, they should be
included as **AdditionalFiles** in your project. This is controlled by the file's build action in Visual Studio
or by an `<ItemGroup>` in your `.csproj`:

```xml
<ItemGroup>
  <AdditionalFiles Include="**\*.zui" />
  <AdditionalFiles Include="**\*.zth" />
</ItemGroup>
```

If you do not set the build action to "C# analyzer" (or "AdditionalFiles"), the generator will not see the file.
Some project templates may add these rules automatically, but if your files are not being picked up, check the
build action or add the above ItemGroup to your project file.

### Control content loading

Controls have two kinds of content in the loader lifecycle:

- Template content is the control's own ZUI-defined visual structure.
- Parent content is supplied by the containing control through the child control's `$content`.

The calls happen in this order:

```text
Generated control constructor
        |
        v
Loader.Load(control, control's own ZUI JSON)
        |
        +--> merge properties and layout
        |
        +--> LoadTemplateContent(template content)
        |       default: add children to View
        |       custom:  add or organize control-owned children
        |
        +--> apply data properties
        |
        +--> initialize generated named-control fields and DataContext

Parent creates a child from its $content
        |
        v
Loader.CreateControl(child properties, context)
        |
        +--> construct child and complete its Loader.Load sequence
        |
        +--> merge parent properties and layout
        |
        +--> LoadParentContent(parent content)
                default: add children to ContentHost
                custom:  override only when parent content needs special handling
```

`Loader.Load` invokes `Controllable.LoadTemplateContent` for the control's own ZUI content. The default implementation
adds that content to `View`. Generated initialization then caches named controls and creates the control's `DataContext`.
When a parent creates a control from its `$content`, `Loader.CreateControl` first constructs and initializes the child,
then applies the child's instance properties and invokes `Controllable.LoadParentContent`.

`ContentHost` controls the destination for parent content; it does not change the call order or the ownership of the
content. `LoadTemplateContent` is for the control-owned structure, while `LoadParentContent` is for content supplied by
the parent. The default `LoadParentContent` implementation creates the supplied child controls and adds their views to
`ContentHost`, which is `View` by default.

`ScrollViewer` uses the default template-loading behavior: its own ZUI content defines the clipped viewport, content
window, and overlay scrollbars. It overrides `ContentHost` so parent content is placed in `_contentWindow`, separate
from the scrollbars. `Window` uses the same pattern: its template defines the title bar, clipped `_windowContent`, and
resize handle, while its `ContentHost` routes parent content to `_windowContent`. Its `title` data property forwards to
the title TextView's `text` property.

This explicit distinction avoids inferring the loading phase from the current child count. It also provides a
foundation for future controls with templates or named content hosts without adding control-specific phase detection to
the loader.

For measurement, arrangement, sizing, and content-extent behavior, see [ScrollViewer](scroll-viewer.md).

## Input and interaction architecture

For the complete pointer-input pipeline, including hit testing, preview and bubble routing, capture, and click
generation, see [Input and pointer events](input-events.md).

Pointer capture is required for controls that continue an interaction after the pointer leaves their bounds. Controls
should capture during pointer-down and clear interaction state from `PointerCaptureLost`; pointer-up is not sufficient
as the only cleanup path. Keep pointer-over and pressed state conceptually separate while capture is active. These
rules apply to interactions such as scrollbar dragging, window movement, and resize handles.

## Editing guidelines

- Prefer minimal allocations and small payloads (WASM download size matters).
- Avoid adding new dependencies unless necessary.
- The source generator (i.e `ZurfurGuiGen`) targets `netstandard2.0`
- The generated code targets `.net 10`

## Where to look first (for AI agents)

- `ZurfurGuiGen/GenerateZui.cs`: source generator entry point; wires up ZUI and ZTH pipelines.
- `ZurfurGuiGen/ZuiInput.cs`: collects data from `.zui` / `.zth` files into `FileInfo`; parses generic `$controller`
  syntax, `where` constraints, and top-level `#comment` into metadata fields.
- `ZurfurGuiGen/ZuiSchema.cs`: parses `$data` bindings, named-control discovery, `#comment` injection per binding, and
  control-name-to-C#-type translation (including generic forms).
- `ZurfurGuiGen/ZuiEmitController.cs`: emits the controller class, `InitializeControl`, `DataContext` property,
  `PropertiesAccessor`, `OnDataContextPropertyChanged`, `SyncAllPropertiesToView`, and `SetDataProperty`; handles generic
  class headers and
  always emits non-generic companion classes for generic controls.
- `ZurfurGuiGen/ZuiEmitData.cs`: emits `<ViewName>Data` concrete data classes; handles generic data classes and top-level
  doc comment propagation.
- `ZurfurGuiGen/ZuiEmitMain.cs`: emits `ZurfurMain.InitializeControls()` — control registration, concrete data-controller
  factory registration, theme registration, and `RunClassConstructor` calls for open generic types and each closed generic
  instantiation.
- `ZurfurGuiGen/ZuiEmit.cs`: shared code-emission helpers.
- `ZurfurGuiGen/Json.cs`: generator JSON parser (does not use `System.Text.Json`); supports `//` line comments and trailing
  commas, captures comments and injects them as `#comment` into adjacent dictionaries, and uses explicit `RemoveKeys`
  calls before generator-only metadata is embedded in generated `.cs` files.
- `ZurfurGui/Loader.cs`: runtime loader, `RegisterControl`, `Load`, `ApplyDataProperties`, concrete data-controller
  registration, and runtime concrete-data factory lookup.
- `ZurfurGui/Styles`: Style and theme property resolution and caching
- `ZurfurGui/Controls/Panel.Control.cs`: all Panel `PropertyKey` definitions (attached properties).
- `ZurfurGui/Controls/*.zui`: view/control definitions and `$data` declarations.
- `docs/combo-box.md`: how the ComboBox control works, how to use it, and how to create custom item renderers.
