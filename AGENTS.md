# Coding agents / AI notes

This repo experiments with a minimal C# GUI stack (WebAssembly + native Windows) using a Model Data View approach.

## Design direction (MDV / MDCV)
	
Goal: replace MVVM-style runtime bindings with **generated, compile-time contracts**.

- **View (V)** declares the data it needs (currently in ZUI JSON `data`; other hosts may use an equivalent XAML `data` section).
- Code generation produces:
  - Code-behind partial controller class (`*.zui.json` → `<ViewName>.Control.g.cs`)
  - a strongly typed **data interface** (`*.zui.json` → `<ViewName>.DataContract.g.cs`)
  - a concrete **data class** that implements it (`*.zui.json` → `<ViewName>.Data.g.cs`)
  - optionally, a **data-command** surface (MDCV) for typed interactions/commands
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

- In ZUI JSON, all type names are PascalCase (including built-in aliases like `Int`, `Bool`, `String`, etc.).
- In ZUI JSON, all field names are camelCase.
- The source generator converts from JSON style to C# style when generating code (field/property names become PascalCase in C#, and built-in type aliases are normalized to the corresponding C# keywords).

Naming notes:

- The generated controller type name is exactly `<ViewName>` (it is not suffixed).
- For view code-behind, use a partial class in `<ViewName>.Control.cs`.
- If `.data` is present, the generator emits:
  - a data contract interface `I<ViewName>Data` in `<ViewName>.DataContract.g.cs`
  - a data implementation class `<ViewName>Data` in `<ViewName>.Data.g.cs` (generated as `partial` if you provide `<ViewName>.Data.cs`)
- For data code-behind/extensibility, use a partial class in `<ViewName>.Data.cs`.
- All generated and user-authored partials for a view are in the same namespace (the namespace comes from the JSON `.namespace`).

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


### 2) Per-view data contract (`*.zui.json` → `<ViewName>.DataContract.g.cs` + `<ViewName>.Data.g.cs`)

- If a view declares a `.data` section, the generator emits:
  - `I<ViewName>Data` in `<ViewName>.DataContract.g.cs`
  - `<ViewName>Data : I<ViewName>Data` in `<ViewName>.Data.g.cs`
- If you provide a matching hand-written `<ViewName>.Data.cs`, the generated data class is emitted as `partial` so you can
  extend it.

### 3) Per-project registry (`*.zui.json` + `*.zss.json` → `ZurfurMain.g.cs`)

- The generator collects all views (`*.zui.json`) and stylesheets (`*.zss.json`) in the project.
- It emits a `static partial class ZurfurMain` with an `InitializeControls()` method that:
  - runs static constructors so control properties get registered
  - registers generated controls with `Loader.RegisterControl(...)`
  - registers styles with `Loader.RegisterStyleSheet(...)`

High-level runtime flow: your app’s entry point calls the generated initialization, then creates a generated controller
(or loads one by name) to build and render the UI.

## Where to look first (for AI agents)

- `ZurfurGuiGen/GenerateZui.cs`: source generator (controller + data contract generation).
- `ZurfurGuiGen/Json.cs`: generator JSON parser (does not use `System.Text.Json`).
- `ZurfurGui/Loader.cs`: runtime loader/registration and JSON deserialization pipeline.
- `ZurfurGui/Controls/*.zui.json`: view/control definitions and `.data` declarations.


