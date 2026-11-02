# Architecture

This repo experiments with a minimal C# GUI stack (WebAssembly + native Windows) using
a **Model Data View (MDV)** approach. This document describes the design direction and the shape of
the system. Details live in the documents listed under [Documentation map](#documentation-map).

## Design direction (MDV)

Goal: replace **MVVM** style runtime bindings with **generated, compile-time view data shapes**.

- The **View (V)** declares the **data (D)** it needs (in ZUI JSON `$data` section).
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
   - May include references to sub-control `DataContext` objects, but only when explicitly declared
	 by the view's `$data` section

**Key principle:** Controls reference data; data may reference other data objects—but **data never
references controls**. The control tree and data graph are independent; the data graph shape is
determined by what the view declares in `$data`.

The generated controller and its generated data type are intentionally related: a view-specific
controller consuming a view-specific data shape is the MDV relationship. The required decoupling is
between data and controls, not between a controller and the data contract it renders.

### Relation to MVVM

This resembles MVVM's visual tree plus ViewModel graph, but MDV does not assume a 1:1 tree shape. The
data side should not reach into controls; integration should happen through declared bindings.

## Visibility and the platform boundary

Application code (for example `TestApp`) should see only the GUI API: controls, views, properties,
input events, and the loader entry points. The pieces that connect the library to an operating
system or browser are an implementation detail shared by the library and its platform hosts.

Hosts are the platform projects: `ZurfurGui.WinForms` and `ZurfurGui.Browser`. Each is referenced
only by the platform stub of an application (for example `TestApp.Windows` and `TestApp.Browser`),
never by the shared application project (`TestApp`).

### What is hidden

These types are `internal` to `ZurfurGui` and are not part of the public API:

- `Renderer`, including its constructor, `RenderFrame`, and statistics. `AppWindow.Renderer` is
  internal too.
- `OsWindow`, `OsCanvas`, and `OsContext`, the interfaces a host implements.
- `OsPointerEvent` and `OsWheelEvent`, the raw input events a host sends to the library. Their
  `Type` is a string that matches the platform's own event names, so a new platform can pass its
  names straight through. They are converted to the public `PointerEvent` and `WheelEvent` inside
  the library.
- `OsRenderBuffer` and `OsRenderCommand`, the encoded drawing commands a host plays back in
  `OsContext.Present`.
- The `MeasureContext` constructor. The `MeasureContext` class itself is public because any
  control may measure text, but only the renderer creates one.

### What is visible

Everything a control or application uses stays public: `View`, `Controllable`, `AppWindow`,
`MeasureContext` (its members), `RenderContext`, the pointer, wheel, and gesture types, and the
base types such as `Point`, `Size`, `Rect`, and `Color`.

### How it works

- `ZurfurGui.csproj` declares `InternalsVisibleTo` for `ZurfurGui.WinForms` and
  `ZurfurGui.Browser`. Those two assemblies can use the internal types. No other assembly can.
- The host classes that implement the `Os*` interfaces (for example `WinCanvas` and
  `BrowserCanvas`) must be `internal`, because a public type cannot implement an internal
  interface.
- A public member must never expose an internal type. The compiler enforces this (CS0051 and
  related errors), so the build catches any leak.
- Hosts start the library by creating a `Renderer` and calling `RenderFrame` from the platform's
  frame timer. See `ZurfurGui.WinForms/FormZurfurGui.cs` and `ZurfurGui.Browser/BrowserStart.cs`.

### Generated-code contract

Generated code is compiled into the application's assembly, so it cannot use `InternalsVisibleTo`.
Its hooks are public but isolated and hidden from IntelliSense:

- `Controllable` is the application-facing interface: `View`, `ContentHost`, `SetDataProperty`,
  `OnAttach`, and `OnDetach`.
- `ZurfurGui.Generated.ZuiControllable` extends `Controllable` with the members only generated code
  implements (`TypeName`, `TypeNamespace`, `TypeUses`, `ZuiPropertyInfo`).
- `ZurfurGui.Generated.ZuiRuntime` has the loader calls generated code makes (`Load`,
  `RegisterControl`, `RegisterDataController`).
- `Loader` keeps only the JSON serialization members public (`JsonSerializerOptions` and
  `RegisterJsonTypeInfoResolver`). `Init`, `CreateControl`, and the rest are internal; the platform
  hosts reach `Init` through `InternalsVisibleTo`.

The generator and library are versioned together. See [Code generation](code-generation.md).

### Adding a platform

A new platform must be added to the `InternalsVisibleTo` list in `ZurfurGui.csproj` and must
implement `OsWindow`, `OsCanvas`, and `OsContext`. Third-party platforms outside this repo are not
supported yet. Supporting them would need a separate public platform project. See the A1 to A3
items in [cleanup.md](cleanup.md) and [Future improvements](future-improvements.md).

## Documentation map

Each fact has one owning document. Other documents link to it rather than repeating it.

| Document | Owns |
|---|---|
| [ZUI and ZTH file format](zui-format.md) | JSON5 syntax, reserved `$` keys, naming conventions, comment handling. |
| [Code generation](code-generation.md) | What the generator emits, filename and namespace detection rules, `ZurfurMain.g.cs`, generic controls, diagnostics, generator source map. |
| [Property binding](property-binding.md) | The `$data` section, binding categories, `DataContext` vs `Properties`, collections, runtime data behavior. |
| [Initialization](initialization.md) | Startup sequence, `Loader` registries, per-control initialization, template and parent content loading. |
| [Input and pointer events](input-events.md) | Pointer ingress, hit testing, preview/bubble routing, capture, click recognition. |
| [Gesture recognizer](gesture-recognizer.md) | Gesture recognition details. |
| [Styles and themes](styles-and-themes.md) | `.zth` tokens, theme activation and resolution. |
| [Controls](controls.md) | The built-in control set and how to use it. |
| [ComboBox](combo-box.md), [ScrollViewer](scroll-viewer.md) | Control-specific behavior. |
| [Future improvements](future-improvements.md) | Everything not yet implemented, deferred, or likely to change. |

## Subsystem summary

- **Configuration and code generation.** `.zui` and `.zth` files are parsed by a Roslyn source
  generator at build time into controllers, data classes, and a per-project registry. See
  [ZUI and ZTH file format](zui-format.md) and [Code generation](code-generation.md).
- **Binding.** A view's `$data` section declares data-only, style-aware, view-only, and forwarded
  values. Data flows through the concrete data class, and the generated controller mirrors
  style-aware values to the view. See [Property binding](property-binding.md).
- **Startup and loading.** The application starts the platform, `Loader.Init` calls the generated
  registration, and `Loader.Load` builds each control's tree from its embedded JSON. See
  [Initialization](initialization.md).
- **Input.** Platform input is converted to logical pointer events and routed through the same view
  tree that is rendered. Controls that continue an interaction after the pointer leaves their bounds
  must use pointer capture and clean up in `PointerCaptureLost`. See
  [Input and pointer events](input-events.md).
- **Styles and themes.** Theme tokens resolve style-aware properties at read time. See
  [Styles and themes](styles-and-themes.md).

## Editing guidelines

- Prefer minimal allocations and small payloads (WASM download size matters).
- Avoid adding new dependencies unless necessary.
- The source generator (i.e `ZurfurGuiGen`) targets `netstandard2.0`
- The generated code targets `.net 10`
- Update the owning document (see the table above) when behavior changes. Put anything that is not
  yet implemented, deferred, or likely to become stale in
  [Future improvements](future-improvements.md) and link to it.

## Where to look first (for AI agents)

Paths are relative to `src/`. For the generator source files, see the source map in
[Code generation](code-generation.md#source-map).

- `ZurfurGui/Loader.cs`: runtime loader, `RegisterControl`, `Load`, `ApplyDataProperties`, concrete
  data-controller registration, and runtime concrete-data factory lookup.
- `ZurfurGui/Base/Controllable.cs`: the application-facing controller interface, including
  `ContentHost`. `ZurfurGui/Generated` has the generated-code-only `ZuiControllable` and `ZuiRuntime`.
- `ZurfurGui/Base/View.cs`: the view, property storage, style lookup, and property-key owner checks.
- `ZurfurGui/Styles`: Style and theme property resolution and caching, plus the built-in `.zth`
  themes.
- `ZurfurGui/Controls/Panel.zui`: the `$`-prefixed global Panel properties. Their `PropertyKey`
  definitions are generated from this file (for example, `Panel.Name`).
- `ZurfurGui/Controls/*.zui`: view/control definitions and `$data` declarations.
- `ZurfurGui/ZurfurMain.cs`: the library's own `MainApp()` that calls the generated
  `InitializeControls()`.
- `ZurfurGui.WinForms/WinStart.cs` and `ZurfurGui.Browser/BrowserStart.cs`: platform entry points.
- `docs/combo-box.md`: how the ComboBox control works, how to use it, and how to create custom item
  renderers.
