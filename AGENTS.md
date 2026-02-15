# Coding agents / AI notes

This repo experiments with a minimal C# GUI stack (WebAssembly + native Windows) using a Model Data View approach.

## Design direction (MDV / MDCV)
	
Goal: replace MVVM-style runtime bindings with **generated, compile-time contracts**.

- **View (V)** declares the data it needs (currently in ZUI JSON `data`; other hosts may use an equivalent XAML `data` section).
- Code generation produces:
  - Code-behind partial controller class (`*.zui.json` → `<ViewName>.g.cs`)
  - TBD: a strongly typed **data interface**
  - TBD: a concrete **data class** that implements it
  - TBD: optionally, a **data-command** surface (MDCV) for typed interactions/commands
- You can:
  - use the generated data class as-is (e.g., deserialize JSON directly into it), or
  - extend it as a `partial` class to map to/from the domain **model (M)**.

## Terminology

- **Model (M):** domain logic/types; should remain UI-agnostic when practical.
- **Data (D):** view-shaped data contract + implementation generated from the view’s declared needs.
- **Commands (C) (optional):** typed interaction surface generated alongside data.
- **View (V):** the renderer/layout/input layer that consumes the generated data interface.

## Editing guidelines

- Prefer minimal allocations and small payloads (WASM download size matters).
- Avoid adding new dependencies unless necessary.
- Generated code should not be hand-edited; change the source schema/generator instead.

## Code generation overview (`ZurfurGuiGen.GenerateZui`)

This repo uses a Roslyn incremental source generator (`ZurfurGuiGen/GenerateZui.cs`) to turn UI JSON files into C#.
It generates two kinds of output:

### 1) Per-view controller (`*.zui.json` → `<ViewName>.g.cs`)

- Each `*.zui.json` file describes a view/control.
- The generator emits a controller class named after the file (for example, `MyView.zui.json` → `MyView.g.cs`).
- The generated controller embeds the JSON, calls `Loader.Load(...)` to build the view tree, and exposes named child
  controls as fields (based on `.name` in the JSON).
- If you provide a matching hand-written `<ViewName>.cs`, the generated class becomes `partial` so you can add
  code-behind without editing generated code.

### 2) Per-project registry (`*.zui.json` + `*.zss.json` → `ZurfurMain.g.cs`)

- The generator collects all views (`*.zui.json`) and stylesheets (`*.zss.json`) in the project.
- It emits a `static partial class ZurfurMain` with an `InitializeControls()` method that:
  - runs static constructors so control properties get registered
  - registers generated controls with `Loader.RegisterControl(...)`
  - registers styles with `Loader.RegisterStyleSheet(...)`

High-level runtime flow: your app’s entry point calls the generated initialization, then creates a generated controller
(or loads one by name) to build and render the UI.
