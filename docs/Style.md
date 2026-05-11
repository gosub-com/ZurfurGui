# ZurfurGui Style System

This document describes how the ZurfurGui style system works — aimed at both human developers and AI code assistants
working in this repository.

## Overview

ZurfurGui uses a CSS-inspired styling system called **ZSS** (Zurfur Style Sheets). Styles are defined in JSON files
with the `.zss.json` extension, detected automatically by the source generator (`ZurfurGuiGen`), and registered at
startup. At runtime, the style engine resolves visual properties for each view by matching style rules against the
view's **classes** and **pseudo-classes**.

Key characteristics:
- **Declarative**: styles live in JSON files, not in C# code.
- **Cascading**: multiple style rules can contribute to a single property; more-specific rules take precedence.
- **Mergable**: struct-like properties (e.g. `FontProp`, `ThicknessProp`) can be *partially* filled in by successive
  rules before falling back to a hard-coded default.
- **Dynamic**: pseudo-classes such as `:IsPointerOver`, `:IsPressed`, and `:IsDarkMode` are evaluated live at render
  time, enabling hover/press/theme effects without any C# code.

## File Format

A style sheet is a single JSON object with:
- `"name"` — a unique identifier used to reference the sheet.
- `"styles"` — an ordered array of rule objects.

`// line comments` are supported (they are stripped before JSON parsing).

### Example

```json
{
    "name": "MyTheme",
    "styles": [
        {
            ".selectors": "Button",
            ".backgroundColor": "#DDDDDD",
            ".borderColor": "#606060",
            ".borderWidth": 2,
            ".padding": { "left": 4, "top": 2, "right": 4, "bottom": 2 }
        },
        {
            ".selectors": "Button:IsPointerOver",
            ".backgroundColor": "#BEE6FD"
        },
        {
            ".selectors": "Button:IsDarkMode",
            ".backgroundColor": "#505050"
        }
    ]
}
```

## Applying Styles

Style sheets are activated on a subtree by setting `.useStyles` on any ancestor view:

```json
{
    ".controller": "MyCard",
    ".useStyles": [ "ZurfurDefault" ],
    ".content": [ ... ]
}
```

Individual views opt into style rules by declaring `.classes`:

```json
{
    ".controller": "TextView",
    ".classes": [ "TextView", "Button.Text" ]
}
```

A view can carry **multiple classes**; the style engine tests each selector against each class.

## Selectors

Each style rule has a `.selectors` string (or an array of strings). A selector has the form:

```
ClassName[:PseudoClass1[:PseudoClass2...]]
```

### Supported Pseudo-Classes

| Pseudo-class      | Condition                                        |
|-------------------|--------------------------------------------------|
| `IsPointerOver`   | Pointer is currently over the view               |
| `!IsPointerOver`  | Pointer is **not** over the view                 |
| `IsPressed`       | View is in a pressed state                       |
| `!IsPressed`      | View is **not** pressed                          |
| `IsDarkMode`      | The `AppWindow` has dark mode enabled            |
| `!IsDarkMode`     | The `AppWindow` has dark mode disabled           |

A rule matches when the selector's base name matches one of the view's classes **and** all pseudo-class conditions
are satisfied.

### Compound Class Names

Dot-separated class names like `Button.Text` are treated as a single string token — they are matched literally
against the values in a view's `.classes` array. They are a naming convention to indicate that a rule targets a
nested element (e.g. the text label inside a button).

## Property Resolution Order

When the runtime needs the value of a styled property for a view it follows this order:

1. **Directly set property** on the view (set via `View.SetProperty` or in the `.zui.json` file). If the property
   type implements `IMergable<T>` and is not `IsComplete`, continue to merge from styles. Otherwise, return
   immediately.
2. **Style lookup**: walk up the view tree from the view to the root. For each ancestor that has `.useStyles`,
   iterate through the referenced style sheets and their rules (in **reverse declaration order** — last rule wins for
   equal specificity). Collect matching values.
3. **Merge** (for `IMergable<T>` types): combine partial values field-by-field using `Or()` — the first non-null
   field value encountered wins.
4. **StyleDefault**: the hard-coded default declared in the `PropertyKey<T>` constructor.

Style lookup results are **cached** on the view using an offset key range (`PROPERTY_STYLE_CACHE_BEGIN`). The cache
is invalidated whenever a pseudo-class state changes (via `ViewFlags.ReStyleThis`) or a `UseStyles`/`Classes`
property changes (via `ViewFlags.ReStyleDown`).

## Styleable Properties

Any registered `PropertyKey<T>` can be used in a style rule — the styling engine is fully generic. There are two
categories of styleable properties:

- **Panel attached properties** — defined on `Panel` and shared by all views. Their JSON key names begin with `.`
  (e.g. `.backgroundColor`). The `.` is part of the registered key name, not a special syntax.
- **Control data properties** — defined per-controller via the `.data` section of a `.zui.json` file and registered
  by the source generator. Their key names use the form `ControllerName.propertyName` (camelCase after the dot,
  e.g. `TextView.color`). See [Data Property Styling](#data-property-styling) below.

Common Panel attached properties:

| JSON key             | C# Type         | Description                          |
|----------------------|-----------------|--------------------------------------|
| `.backgroundColor`   | `Color`         | Background fill color                |
| `.borderColor`       | `Color`         | Border stroke color                  |
| `.borderWidth`       | `double`        | Border stroke width                  |
| `.borderRadius`      | `double`        | Corner radius                        |
| `.padding`           | `ThicknessProp` | Inner padding (left/top/right/bottom)|
| `.margin`            | `ThicknessProp` | Outer margin                         |
| `.align`             | `AlignProp`     | Horizontal/vertical alignment        |
| `.sizeRequest`       | `SizeProp`      | Requested width/height               |
| `.sizeMin`           | `SizeProp`      | Minimum width/height                 |
| `.sizeMax`           | `SizeProp`      | Maximum width/height                 |
| `.isVisible`         | `bool`          | Visibility                           |

Control-specific properties use the form `TypeName.propertyName`. For `TextView`:

| JSON key         | C# Type    | Description              |
|------------------|------------|--------------------------|
| `TextView.color` | `Color`    | Text color               |
| `TextView.font`  | `FontProp` | Font name and/or size    |
| `TextView.text`  | `TextLines`| Text content             |

> **Note for AI assistants**: property keys in style rules must exactly match the registered `PropertyKey<T>.Name`
> string. For Panel attached properties the leading `.` is part of the name (e.g. `.backgroundColor`, not
> `backgroundColor`). For data properties the name is `ControllerName.propertyName` in camelCase
> (e.g. `TextView.color`, `TextView.font`, `TextView.text`).

## Data Property Styling

Data properties declared in a `.zui.json` `.data` block are registered as `PropertyKey<T>` instances by the source
generator. Whether a data property participates in style lookup depends on its nullability:

- **Nullable** (`?type` in `.zui.json`, e.g. `"type": "?Color"`): when the data context value is `null`, the
  generated code calls `View.RemoveProperty(key)`, which removes the direct value and allows the style engine to
  resolve the property from the active style sheets. When the value is non-null it calls `View.SetProperty(key,
  value)`, which overrides the style.
- **Non-nullable** (`type` without `?`): the generated code always calls `View.SetProperty(key, value)`, so the
  property always overrides any matching style rule and cannot be styled from a style sheet.

All three `TextView` data properties (`text`, `font`, `color`) are declared nullable, which is why they can be set
from style sheets. Their registered key names are:

| JSON key         | C# Type     | Nullable | Description           |
|------------------|-------------|----------|-----------------------|
| `TextView.color` | `Color`     | yes      | Text color            |
| `TextView.font`  | `FontProp`  | yes      | Font name and/or size |
| `TextView.text`  | `TextLines` | yes      | Text content          |

> **Note for AI assistants**: the property name after the dot is **camelCase** as declared in `.zui.json`
> (e.g. `TextView.text`, not `TextView.Text`).

## Mergable Properties

Types implementing `IMergable<T>` (e.g. `FontProp`, `SizeProp`, `ThicknessProp`, `AlignProp`, `DoubleProp`) support
partial specification. Each field is independently nullable; the style engine merges values from successive rules
until `IsComplete` returns `true`.

Example — a view has two classes `["TextView", "StyleC1", "StyleC2"]` and two rules:

```json
{ ".selectors": "StyleC1", "TextView.font": { "size": 28.0 } }
{ ".selectors": "StyleC2", "TextView.font": { "name": "Times New Roman" } }
```

The resolved font is `{ name: "Times New Roman", size: 28.0 }` — both fields contributed by different rules.

## Built-in Style Sheets

The `ZurfurGui` library ships one built-in style sheet that is always registered:

| Name            | Description                                                  |
|-----------------|--------------------------------------------------------------|
| `ZurfurDefault` | Default theme: light-mode rules plus `:IsDarkMode` overrides |

Both light and dark variants live in a single file (`ZurfurDefault.zss.json`). Light-mode rules use plain selectors;
dark-mode overrides use `:IsDarkMode` pseudo-classes. This is the recommended pattern for any theme that needs to
support both modes.

## Defining a Custom Style Sheet

1. Create a file named `MyTheme.zss.json` anywhere in your project.
2. Add it to the `.csproj` as an `AdditionalFile`.
3. Set `"name"` to a unique identifier (e.g. `"MyTheme"`).
4. Add rules under `"styles"`.
5. Reference it from a `.zui.json` via `.useStyles: ["MyTheme"]`.

The source generator will automatically emit the `RegisterStyleSheet` call; no further C# code is needed.

## Architecture Notes

- **Style resolution lives in `Style.cs`** (`ZurfurGui.Styles`). `GetStyle<T>` is the main entry point called from
  `View.GetStyle<T>`.
- **`StyleSheet`** (`ZurfurGui.Property`) is the deserialized form of a `.zss.json` file: it holds a `Name` and a
  `Properties[]` (array of rule objects).
- **`Loader.RegisterStyleSheet(json)`** deserializes the JSON string and stores the `StyleSheet` by name.
  `Loader.GetStyleSheet(name)` retrieves it.
- **`Panel.Classes`**, **`Panel.UseStyles`**, and **`Panel.Selectors`** are the three properties that drive style
  matching.
- Style cache invalidation is triggered by `ViewFlags.ReStyleThis` (only the view itself) and
  `ViewFlags.ReStyleDown` (the view and all descendants).
- When adding a new styleable property, declare a `PropertyKey<T>` with the appropriate `ViewFlags` (`ReDraw` for
  appearance-only, `ReMeasure` for layout impact, `ReStyleDown` for style-tree properties).

## Known Issues

### 1. `UseStyles` is not inherited by floating panels

`Style.EnumerateStyledValues` finds active style sheets by walking **up the view tree** looking for ancestors that
have `Panel.UseStyles` set. Floating panels (dropdowns, tooltips) are parented to `_floatingWindows` under
`AppWindow`, so they are outside the originating form's subtree. Only `AppWindow`'s own `UseStyles` (e.g.
`"ZurfurDefault"`) is visible to them; any custom stylesheet declared on the form (e.g. `"ComboBoxItemBadge"`) is
not found, so item styles silently resolve to defaults.

**Current workaround** (in `ComboBox.Control.cs`): `OpenDropdown` collects all `UseStyles` names from the
`ComboBox`'s ancestor chain and stamps them onto the popup panel before adding item views.

**Proper fix**: `Style.EnumerateStyledValues` should search all globally-registered style sheets directly from
`Loader`'s dictionary instead of requiring `UseStyles` to be set in the ancestor chain. Every `*.zss.json` in the
project is already registered globally at startup, so the ancestor-walk requirement is redundant and the `UseStyles`
property (and `.useStyles` in `.zui.json`) could be removed entirely.

### 2. `ComboBox.DropdownItem` class clobbered item's own classes

`OpenDropdown` originally called `item.View.SetProperty(Panel.Classes, ["ComboBox.DropdownItem"])`, which replaced
whatever classes `InitializeControl` had already set on the item's root view (e.g. `"ComboBoxItemBadge"`). This
stripped the item's own selector, breaking its border, color, and padding styles in the popup while they continued
to work correctly in the selected-item slot.

**Fix applied** (in `ComboBox.Control.cs`): each popup item is now wrapped in a thin host `Panel` that owns the
`"ComboBox.DropdownItem"` class and click handler. The item's view is added as a child of the wrapper and is never
modified, so its own classes and styles are fully preserved.

### 3. Custom item types cannot inherit theme-specific hover colors

The core theme stylesheets (`ZurfurDefault.zss.json`, `ZurfurCherry.zss.json`) live in `ZurfurGui` and cannot
reference app-defined item types like `ComboBoxItemBadge`. As a result, switching to the Cherry theme correctly
changes window chrome and built-in controls to pink tones, but custom item types retain the hover color that was
hardcoded in their own stylesheet (e.g. `ComboBoxItemBadge:IsPointerOver` stays blue on Cherry instead of turning
pink like `ComboBoxItemText`).

**Current workaround**: create a companion theme-override stylesheet for the custom item type (e.g.
`ComboBoxItemBadgeCherry.zss.json`) containing only the selector overrides that differ from the default theme:

```json
{
    "name": "ComboBoxItemBadgeCherry",
    "styles": [
        { ".selectors": "ComboBoxItemBadge:IsPointerOver",           ".backgroundColor": "#F8D8E0" },
        { ".selectors": "ComboBoxItemBadge:IsDarkMode:IsPointerOver", ".backgroundColor": "#4A2535" }
    ]
}
```

Then, in the form's theme-change handler, include both the base stylesheet and the override when Cherry is active,
and drop the override for the default theme. Because `EnumerateStyledValues` iterates `UseStyles` in reverse, the
last-listed sheet wins, so Cherry overrides take precedence:

```csharp
// in ThemeComboBox_PropertyChanged (FormTestComboBox.Control.cs)
case 0: /* default light */ View.SetProperty(Panel.UseStyles, new TextLines(["ComboBoxItemBadge"])); break;
case 1: /* default dark  */ View.SetProperty(Panel.UseStyles, new TextLines(["ComboBoxItemBadge"])); break;
case 2: /* cherry light  */ View.SetProperty(Panel.UseStyles, new TextLines(["ComboBoxItemBadge", "ComboBoxItemBadgeCherry"])); break;
case 3: /* cherry dark   */ View.SetProperty(Panel.UseStyles, new TextLines(["ComboBoxItemBadge", "ComboBoxItemBadgeCherry"])); break;
```

**Proper fix (future)**: introduce **style variables** similar to WinUI 3 / Fluent Design tokens (e.g.
`--item-hover-background`). Theme stylesheets would define the variable values; item stylesheets would reference
them by name instead of hardcoding a color. Custom item types then automatically pick up the correct hover color for
whichever theme is active, with no per-type companion stylesheets or handler code required. The style variable
mechanism would be resolved during `GetStyle<T>` after selector matching, before merging with the property default.
