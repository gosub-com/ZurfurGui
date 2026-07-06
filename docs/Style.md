# Style and Theming System

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
- **Dynamic**: pseudo-classes such as `:IsPointerOver` and `:IsPressed` are evaluated live at render
  time, enabling hover/press/theme effects without any C# code.

## Themes and Tokens

Fluent style tokens were recently added.

This document has not yet been updated to describe them.

## Style Sheets (`.zss` files)

Each control should ship with its own style sheet (.`zss` file) containing default styles for the control.

A style sheet is a single JSON object with:
- `"name"` — a unique identifier used to reference the sheet.
- `"styles"` — an ordered array of rule objects.

`// line comments` are supported (they are stripped before JSON parsing).

### Example (see `Button.cs`)

```json
{
    "name": "Button",
    "styles": [
        {
            ".selectors": "Button",
            ".borderWidth": "@stroke.width.default",
            ".borderRadius": "@radius.corner.medium",
            ".borderColor": "@color.interactive.primary.stroke",
            ".backgroundColor": "@color.interactive.primary.background",
            ".padding": "@padding.button"
        },
        {
            ".selectors": "Button:IsPointerOver",
            ".borderColor": "@color.interactive.primary.stroke.hover",
            ".backgroundColor": "@color.interactive.primary.background.hover"
        },
        {
            ".selectors": "Button:IsPressed",
            ".borderColor": "@color.interactive.primary.stroke.pressed",
            ".backgroundColor": "@color.interactive.primary.background.pressed"
        },
        {
            ".selectors": "Button.Text",
            "TextView.color": "@color.text.on-primary"
        }
    ]
}
```

## Property String Formats for Common Types

Several style properties use a concise string format for values. Below are the supported formats for each property type, with examples.

### Color
- **Format:** CSS hex, rgb, or named.
- **Examples:**
  - `.backgroundColor: "#FF0000"` (red)
  - `.backgroundColor: "#FF000080"` (red with 50% opacity)
  - `.borderColor: "255,0,0"`
  - `.borderColor: "255,0,0,128"` (red with 50% opacity)
  - `.color: "red"`

### Point
- **Format:** `"x: value; y: value"` or a single number for both.
- **Examples:**
  - `.offset: "x: 10; y: 20"`
  - `.offset: "8"` (sets both x and y to 8)

### Size
- **Format:** `"width: value; height: value"` or a single number for both.
- **Examples:**
  - `.sizeMin: "width: 160; height: 80"`
  - `.sizeRequest: "24"` (sets both width and height to 24)

### Thickness
- **Format:** `"left: value; top: value; right: value; bottom: value"`, or use `"vertical"`/`"horizontal"` for convenience. 
- **Examples:**
  - `.padding: "left: 3; top: 10; right: 3; bottom: 10"`
  - `.margin: "vertical: 8; horizontal: 4"`
  - `.margin: "8"`

### Align
- **Format:** `"horizontal: value; vertical: value"`
- **Examples:**
  - `.align: "horizontal: center; vertical: top"`
  - `.align: "vertical: bottom"`

### Font
- **Format:** `"name: FontName; size: value"` (additional fields may be supported in the future)
- **Examples:**
  - `TextView.font: "name: Arial; size: 14"`
  - `TextView.font: "size: 20"`

**Notes:**
- Key-value pairs are separated by semicolons (`;`).
- Keys and values are case-insensitive; whitespace is ignored around them.
- Unset fields are omitted and treated as `null`.
- Malformed strings or unknown keys will result in a parse error.


## Applying Styles

Style sheets are automatically registered at startup by the source generator — no explicit activation is
required. Every `.zss.json` file in a project is detected and an `Style.RegisterStyleSheet(...)` call is emitted
into `ZurfurMain.g.cs`. At runtime, `Style.EnumerateStyledValues` consults all registered sheets globally.

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
2. **Style lookup**: iterate through all globally registered style sheets, giving active theme sheets
   the highest priority (they are consulted last so their values win for non-mergable types). Rules are
   evaluated in **reverse declaration order** within each sheet — the last rule wins for equal specificity.
   Collect matching values.
3. **Merge** (for `IMergable<T>` types): combine partial values field-by-field using `Or()` — the first non-null
   field value encountered wins.
4. **StyleDefault**: the hard-coded default declared in the `PropertyKey<T>` constructor.

Style lookup results are **cached** on the view using an offset key range (`PROPERTY_STYLE_CACHE_BEGIN`). The cache
is invalidated whenever a pseudo-class state changes (via `ViewFlags.ReStyleThis`) or a `Classes`
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
{ ".selectors": "StyleC1", "TextView.font": "size: 28" }
{ ".selectors": "StyleC2", "TextView.font": "name: Times New Roman" }
```

The resolved font is `{ name: "Times New Roman", size: 28.0 }` — both fields contributed by different rules.

