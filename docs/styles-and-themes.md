# Style and Theming System

## Overview

This document explains how theme tokens are declared, applied, and resolved.

ZurfurGui uses a **theme token system** where visual properties reference named tokens
defined in theme files. Theme tokens are resolved at runtime by walking up the view
hierarchy, allowing flexible theming at any level—from the entire application down to
individual controls.

The key components are:

- **Theme files** (`.zth`) define named tokens (color, spacing, fonts, etc.)
- **View files** (`.zui`) reference tokens using `"${token.name}"` syntax
- **Theme resolution** walks up the view chain searching for active themes
- **Token fallbacks** using the `|` operator enable third-party controls to gracefully
  degrade

## Theme Files (`.zth`)

`.zth` means Zurfur Theme. Theme files use the modified JSON5 syntax described in
[ZUI and ZTH File Format](zui-format.md). Each theme has a name and a dictionary of variables.

```
{
    name: "ZurfurDefault",
    variables: {
        color.surface.canvas: "#EAF4FB",
        color.text.primary: "#1F1F1F",
        spacing.medium: "8",
        radius.corner.medium: "4",
        color.interactive.primary.stroke: "isPressed ? #005A9E; isPointerOver ? #0078D4; #909090"
    }
}
```

### Token Features

**Conditional expressions**: Tokens can respond to view states using
`condition ? value; alternativeValue` syntax:

```
color.interactive.primary.background: "isPressed ? #A6D2F2; isPointerOver ? #D2E8FA; #C6E0F7",
color.scroll.thumb.background: "isPressed ? #005A9E; ScrollBar.isExpanded ? #6A9FD0; #8BB4DC80"
```

Supported conditions:
- `isPointerOver` / `!isPointerOver`
- `isPressed` / `!isPressed`
- Any registered boolean property using its exact name, such as `ScrollBar.isExpanded`
- A leading `!` negates any supported condition

Registered boolean property conditions read the control's current runtime value rather than resolving
the property as a style. Conditions must name a property exactly; unknown properties and non-boolean
properties are invalid conditions.

Multiple expressions are separated by semicolons, evaluated in order until a matching
condition is found. The last value (without a condition) serves as the default.

**Property merging**: Tokens can contain partial property specifications that merge
together using the `IMergable` interface. For example, spacing tokens can define
individual edges:

```
spacing.horizontal.medium: "left:8,right:8",
spacing.vertical.small: "top:4,bottom:4"
```

These merge when both are referenced via `|`, creating
`"left:8,right:8,top:4,bottom:4"`.

### Built-in Themes

ZurfurGui includes several base themes:

- **ZurfurBase**: Common tokens that rarely change (spacing, corner radii, stroke
  widths, default sizes)
- **ZurfurDefault**: Light theme with Fluent 2-inspired colors
- **ZurfurDefaultDark**: Dark variant
- **ZurfurCherry**: Alternative light theme
- **ZurfurCherryDark**: Alternative dark theme

`ZurfurBase` and `ZurfurDefault` act as ultimate fallbacks during token resolution (see
below).

## Using Theme Tokens in Views

In `.zui` files, wrap token names with `${}` to reference theme tokens:

```
{
    $controller: "Button",
    $borderWidth: "${stroke.width.default}",
    $borderRadius: "${radius.corner.medium}",
    $borderColor: "${color.interactive.primary.stroke}",
    $backgroundColor: "${color.interactive.primary.background}"
}
```

When a view requests a property, its token expression is resolved using the active themes.

### Token Fallback with `|`

Multiple token names can be specified with `|`. This is critical for third-party
controls that define their own tokens but want to fall back to standard system tokens if
the user's theme doesn't define the custom ones:

```
$padding: "${spacing.my-control-inner-padding | spacing.horizontal.small | spacing.vertical.extra-small}"
```

First, the custom token `spacing.my-control-inner-padding` is tried.  If the application has
supplied the token, it is taken.  If not, the next tokens are tried in order. If 
`spacing.horizontal.small` provides a partial value (e.g., only `left` and `right`), 
`spacing.vertical.extra-small` can complete it by providing `top` and `bottom`. If the first 
token fully resolves the property, the rest are ignored.

The `|` operator combined with `IMergable` properties enables powerful composition:

```
TextView.font: "${style.test.font.c1 | style.test.font.c2}"
```

If `font.c1` defines `size:28` and `font.c2` defines `name:Times New Roman`, the
resolved font has both size and name.

### Providing Defaults for `styled` Properties

For a control property declared with `bind: "styled"`, provide its value through the control's theme-token
mapping and include a fallback token that is defined by the built-in themes. This lets
consumers override a control-specific token without requiring every theme to define it. If the
optional token is absent, the fallback supplies a usable value:

```
{
    ScrollBar.thumbEdgeInset: "${size.scrollbar.thumb.edge-inset | spacing.extra-small}",
    $data: {
        thumbEdgeInset: {
            type: "Double",
            bind: "styled",
            flags: "render"
        }
    }
}
```

Here, `size.scrollbar.thumb.edge-inset` is an optional, control-specific token; `spacing.extra-small` is
the guaranteed fallback defined in `ZurfurBase`. The styled-only data item has no `default`: its
value comes from the token mapping. For every `styled` property, use this pattern rather than
putting a `default` on the data item. Choose a fallback with a compatible value type and ensure it
exists in a built-in theme, so the control works when the optional token is not defined.

This guidance is specific to `styled` properties. Keep `default` values on ordinary data, `styledData`,
or `property` bindings when they need an initial runtime value, such as a control's default orientation.

For the distinction between `styled` and `property` bindings, see
[Property Binding](property-binding.md).

## Selecting and Activating Themes

### Setting Application-Wide Themes

To set the theme for the entire application, use the top-level `View`'s `ActiveThemes`
property:

```csharp
view.ActiveThemes = ["MyCustomTheme", "ZurfurDefault"];
```

The first theme has highest priority. Theme resolution then falls back to `ZurfurDefault` and
`ZurfurBase`.

### Setting Themes on Subviews

You can set `ActiveThemes` on any `View` in the hierarchy to apply themes locally:

```csharp
myPanel.View.ActiveThemes = ["SpecialPanelTheme"];
```

Child controls will use this theme (and any themes further up the chain) when resolving
tokens.

## Resolution behavior

An explicitly set property takes precedence over a theme value. Otherwise, each token candidate is
searched through the view's active themes, its parent views, and the built-in fallbacks. Candidate
order takes precedence over theme priority: the first candidate is fully resolved before the next is
tried. Partial `IMergable` values can combine across themes and token candidates. If no candidate
provides a value, resolution fails.

## Creating Custom Themes

To create a custom theme:

1. **Create a `.zth` file** in your project (mark as AdditionalFiles).

2. **Define a unique name and variables**:

```
{
    name: "MyBrandTheme",
    variables: {
        color.surface.canvas: "#F0F0FF",
        color.text.primary: "#000033",
        my.custom.accent: "#FF6600"
    }
}
```

3. **Reference your tokens in `.zui` files**:

```
$backgroundColor: "${my.custom.accent | color.interactive.primary.background}"
```

The `|` fallback ensures your control still works in themes that don't define
`my.custom.accent`.

4. **Activate your theme** at runtime:

```csharp
appWindow.View.ActiveThemes = ["MyBrandTheme"];
```

<!-- INTERNAL -->

## Implementation Section

### Theme resolution algorithm

When a view requests a property, its token expression is resolved using the active themes:

1. **Check explicitly set properties**: If the property was set in code (via
   `View.SetProperty`), use that value (unless it's an incomplete `IMergable`).

2. **Look up the token expression**: Retrieve the token expression from the view's
   `ThemeTokens` dictionary. If it contains `|`, process the token candidates in order.

3. **Resolve each token candidate completely before trying the next**:
   - Starting from the current view, walk up the `Parent` chain. At each view, check its
     `ActiveThemes` in order, with the first theme having the highest priority.
   - If a token is found, deserialize its value. If it contains conditional expressions
     (`isPressed ? ...`), evaluate the condition against the view's state.
   - If the result is an incomplete `IMergable`, merge it with any partial value already
     found and continue searching themes and parent views.
   - After the view hierarchy, check `ZurfurDefault` then `ZurfurBase` if the value is
     still incomplete.
   - If the value is still missing or incomplete, try the next token candidate.

   **Token candidate order takes precedence over theme priority**: the first candidate is
   searched through the full hierarchy and built-in fallbacks before the second candidate
   is considered. Thus, a first candidate found in a built-in theme can take precedence
   over a later candidate found in an active custom theme.

4. **Return the final value**: If no candidate resolves to a value, throw an exception.
   Otherwise, return the resolved (possibly merged) value.

### Theme registration

Themes are registered by generated startup code. See [Code Generation](code-generation.md) and
[Initialization](initialization.md).

