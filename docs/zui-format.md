# ZUI and ZTH File Format

`.zui` files are Zurfur UI configuration files, and `.zth` files are Zurfur Theme configuration
files. Both use a small modified JSON5 format. This document describes the syntax and the reserved
keys. For what the generator does with these files, see [Code generation](code-generation.md). For
the `$data` section, see [Property binding](property-binding.md). For theme files, see
[Styles and themes](styles-and-themes.md).

## Syntax

The format is essentially JSON with comments, and dictionary keys do not need quotes. It is parsed
by `ZurfurGuiGen/Json.cs` in the generator and by `System.Text.Json` at runtime. In addition to
standard JSON, the generator parser supports these JSON5 features:

- **`//` line comments and `/* */` block comments** — a comment outside a string value is skipped.
- **Trailing commas** — a comma after the last property in an object or the last element in an
  array is silently accepted. This matches the JSONC convention used by `tsconfig.json`, VS Code
  `settings.json`, etc.
- **Unquoted dictionary keys** — ASCII identifier keys, including keys beginning with `$` and
  containing `.` or `-`, do not need quotes.
- **Single-quoted strings.**
- **Relaxed decimal numbers** — `+5`, `.5`, and `5.` are accepted.

Not supported: `NaN`, `Infinity`, hexadecimal numbers, the full Unicode identifier grammar, uncommon
JSON5 escape forms, and `null` values.

## Naming conventions

- Keys beginning with `$` are global symbols reserved by ZurfurGUI, such as `$controller`, `$name`,
  `$margin`, and `$width`.
- Type names are PascalCase (including built-in aliases like `Int`, `Bool`, `String`, etc.).
- Field names are camelCase.
- The source generator converts from JSON style to C# style when generating code. Field and property
  names become PascalCase in C#, and built-in type aliases are normalized to the corresponding C#
  keywords.

## Top-level keys of a `.zui` file

| Key | Required | Description |
|---|---|---|
| `$namespace` | Yes | The C# namespace of the generated controller and data classes. |
| `$controller` | Yes | The controller name, which must match the file name. May use the generic form `Name<T> where T : Constraint`. |
| `$use` | No | An array of strings naming extra namespaces searched when resolving child controller names. |
| `$implements` | No | A handwritten C# interface the generated data class implements, such as `IComboBoxItem`. |
| `$data` | No | Declares the view's data shape. See [Property binding](property-binding.md). |
| `$content` | No | The array of child controls that make up the control's own template. |

`$use` must be an array of strings. A single string is an error.

Top-level `$data` entries whose names begin with `$` are only allowed in `Panel.zui`, which defines
the library's global `$`-prefixed properties.

## Comments in generated code

Comments are collected by the generator and emitted into generated code as XML doc comments where
appropriate:

- `//` line comments immediately preceding a `$data` property are emitted as `<summary>` XML doc
  comments on the members generated for that binding.
- A comment at the top of the file, before the top-level object, is emitted as the `<summary>` of
  the generated controller and data classes.

Generator-only metadata (`#comment`, `$namespace`, `$use`, and `$data`) is removed from the JSON
before it is embedded in the generated controller, so it is not seen by the runtime loader.

## `.zth` theme files

A `.zth` file uses the same syntax. Its top-level `name` key must be present and must match the
file name without its extension. Themes are registered by the generated
`ZurfurMain.InitializeControls()`. The token syntax is described in
[Styles and themes](styles-and-themes.md).

## Configuration preference

Prefer setting view properties (for example `$hitTest`, `$layout`, and styled values) in the `*.zui`
file rather than in code-behind. Declarative configuration can be overridden by parents and themes,
while constructor code cannot. Keep code-behind for behavior such as events and layout logic.
