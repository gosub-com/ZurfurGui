# Zurfur Gui

While working on [Zurfur](https://github.com/gosub-com/Zurfur) and
porting the GUI from WinForms to Avalonia, I got to thinking what it would be
like to implement a GUI without using inheritance.  I also wanted to see how big
a minimal C# WebAssembly GUI would be.  So far, the full download is only about 
3.1 megabytes of compressed data.  

You can see the result here: https://gosub.com/zurfurgui

This project is exploring **MDV/MDCV** as an alternative to MVVM.  

## For contributors and AI agents


The following docs cover the internals of this project:

- [Architecture](docs/Architecture.md) — MDV architecture, code generation, and system internals
- [Initialization](docs/Initialization.md) — Startup and initialization procedure for applications using ZurfurGui
- [Property Binding](docs/PropertyBinding.md) — Property binding system
- [Styles and Themes](docs/Style.md) — Style and theme system (style sheets, selectors, pseudo-classes, and property resolution)
- [ComboBox](docs/ComboBox.md) — ComboBox control: usage, generic item types, styling, and internals

## Design Goals

Zurfur Gui is an experiment to see what a minimal C# WebAssembly `Canvas` GUI might look like:

* Runs in browsers and natively on Windows
* Quick and easy to whip up small GUI applications (tools, etc.)
* Uses JSON instead of XML — this is called ZUI JSON
* Small download — uses the built-in platform drawing API (i.e. `Canvas`)

## Model Data View (MDV)	

Zurfur Gui uses a Model Data View (MDV) pattern.  The view (V) declares the
data it needs in the ZUI JSON `.data` section, and then build-time code
generation via the `ZurfurGuiGen` source generator creates a strongly-typed
data interface plus a concrete data class (D).  The view consumes only the
interface.  You can use D directly (for example, deserialize JSON into it) or
extend D as a partial class that maps to/from the domain model. This makes the
view's data requirements explicit and keeps bindings compile-time checked.
