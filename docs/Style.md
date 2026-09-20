# Style and Theming System

This document describes how the ZurfurGui style system works — aimed at both human
developers and AI code assistants working in this repository.

## Overview

ZurfurGui uses a **theme token system** where visual properties reference named tokens
defined in theme files. Theme tokens are resolved at runtime by walking up the view
hierarchy, allowing flexible theming at any level—from the entire application down to
individual controls.

The key components are:

- **Theme files** (`.zth.json5`) define named tokens (color, spacing, fonts, etc.)
- **View files** (`.zui.json5`) reference tokens using `"${token.name}"` syntax
- **Theme resolution** walks up the view chain searching for active themes
- **Token fallbacks** using the `|` operator enable third-party controls to gracefully
  degrade

## Theme Files (`.zth.json5`)

Theme files use JSON5 and define reusable design tokens. Each theme has a name and a dictionary of
variables. Legacy `.zth.json` files remain supported:

```json5
{
    name: "ZurfurDefault",
    variables: {
        "color.surface.canvas": "#EAF4FB",
        "color.text.primary": "#1F1F1F",
        "spacing.medium": "8",
        "radius.corner.medium": "4",
        "color.interactive.primary.stroke": "isPressed ? #005A9E; isPointerOver ? #0078D4; #909090"
    }
}
```

### Token Features

**Conditional expressions**: Tokens can respond to view states using
`condition ? value; alternativeValue` syntax:

```json5
"color.interactive.primary.background": "isPressed ? #A6D2F2; isPointerOver ? #D2E8FA; #C6E0F7"
```

Supported conditions:
- `isPointerOver` / `!isPointerOver`
- `isPressed` / `!isPressed`

Multiple expressions are separated by semicolons, evaluated in order until a matching
condition is found. The last value (without a condition) serves as the default.

**Property merging**: Tokens can contain partial property specifications that merge
together using the `IMergable` interface. For example, spacing tokens can define
individual edges:

```json5
"spacing.horizontal.medium": "left:8,right:8",
"spacing.vertical.small": "top:4,bottom:4"
```

These merge when both are referenced via `|`, creating `"left:8,right:8,top:4,bottom:4"`.

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

In `.zui.json5` files, wrap token names with `${}` to reference theme tokens. Legacy `.zui.json` files
remain supported:

```json5
{
    $controller: "Button",
    $borderWidth: "${stroke.width.default}",
    $borderRadius: "${radius.corner.medium}",
    $borderColor: "${color.interactive.primary.stroke}",
    $backgroundColor: "${color.interactive.primary.background}"
}
```

When the view is loaded, `"${token.name}"` properties are stored separately in a
`ThemeTokens` dictionary on the view, then resolved at runtime through
`ThemeManager.FindStyle`.

### Token Fallback with `|`

Multiple token names can be specified with `|`. This is critical for third-party
controls that define their own tokens but want to fall back to standard system tokens if
the user's theme doesn't define the custom ones:

```json5
$padding: "${spacing.my-control-inner-padding | spacing.horizontal.small | spacing.vertical.extra-small}"
```

First, the custom token `spacing.my-control-inner-padding` is tried.  If the application has
supplied the token, it is taken.  If not, the next tokens are tried in order. If 
`spacing.horizontal.small` provides a partial value (e.g., only `left` and `right`), 
`spacing.vertical.extra-small` can complete it by providing `top` and `bottom`. If the first 
token fully resolves the property, the rest are ignored.

**Example from Window title text color**:

```json5
"TextView.color": "${color.window.menu | color.window.title.foreground}"
```

This tries `color.window.menu` first (custom token), then falls back to
`color.window.title.foreground` if not defined.

The `|` operator combined with `IMergable` properties enables powerful composition:

```json5
"TextView.font": "${style.test.font.c1 | style.test.font.c2}"
```

If `font.c1` defines `size:28` and `font.c2` defines `name:Times New Roman`, the
resolved font has both size and name.

## Selecting and Activating Themes

### Setting Application-Wide Themes

To set the theme for the entire application, use the top-level `View`'s `ActiveThemes`
property:

```csharp
var appWindow = view.AppWindow;
if (appWindow != null)
{
    appWindow.View.ActiveThemes = ["ZurfurDefault"];
}
```

`ActiveThemes` is a `TextLines?` property (essentially a list of strings). You can
activate multiple themes at once; the first theme in the list has the highest priority:

```csharp
appWindow.View.ActiveThemes = ["MyCustomTheme", "ZurfurDefault"];
```

In this case, token resolution will search `MyCustomTheme` first, then `ZurfurDefault`,
before falling back to `ZurfurBase`.

**Note**: The collection initializer syntax `["ThemeName"]` requires `TextLines` to be
publicly accessible. This was fixed by making `TextLinesBuilder` public.

### Setting Themes on Subviews

You can set `ActiveThemes` on any `View` in the hierarchy to apply themes locally:

```csharp
myPanel.View.ActiveThemes = ["SpecialPanelTheme"];
```

Child controls will use this theme (and any themes further up the chain) when resolving
tokens.

## Theme Resolution Algorithm

When a view requests a themed property (e.g., `backgroundColor` mapped to
`"${color.surface.canvas}"`), the `ThemeManager` resolves it as follows:

1. **Check explicitly set properties**: If the property was set in code (via
   `View.SetProperty`), use that value (unless it's an incomplete `IMergable`).

2. **Look up the token expression**: Retrieve the token expression from the view's
   `ThemeTokens` dictionary. If it contains `|`, split it into multiple fallback tokens.

3. **Walk up the view hierarchy**: Starting from the current view, walk up the `Parent`
   chain looking for `ActiveThemes`:
   - For each theme in `ActiveThemes`, look up the token in that theme's `Variables`
     dictionary.
   - If found, deserialize the value. If the value contains conditional expressions
     (`isPressed ? ...`), evaluate the condition against the view's state.
   - If the result is an `IMergable` and incomplete, merge it with any partial value
     already found.
   - If the result is complete (fully resolved), return it.
   - If incomplete, continue to the next theme or parent view.

4. **Fallback to base themes**: After exhausting the view hierarchy, resolve against
   `ZurfurDefault` then `ZurfurBase` (if the token still isn't complete).

5. **Return the final value**: If the token was never found or fully resolved, throw an
   exception. Otherwise, return the resolved (possibly merged) value.


## Theme Registration

Themes are registered at application startup via the generated
`ZurfurMain.InitializeControls()` method. The source generator scans all `.zth.json5`
files in the project (and continues to accept legacy `.zth.json` files) and emits:

```csharp
ThemeManager.RegisterTheme(/* embedded JSON string */);
```

This happens automatically—developers just need to ensure both `.zth.json5` files and any legacy
`.zth.json` files are marked as
`AdditionalFiles` in the project:

```xml
<ItemGroup>
  <AdditionalFiles Include="**\*.zth.json" />
  <AdditionalFiles Include="**\*.zth.json5" />
</ItemGroup>
```

## Creating Custom Themes

To create a custom theme:

1. **Create a `.zth.json5` file** in your project (mark as AdditionalFiles).

2. **Define a unique name and variables**:

```json5
{
    name: "MyBrandTheme",
    variables: {
        "color.surface.canvas": "#F0F0FF",
        "color.text.primary": "#000033",
        "my.custom.accent": "#FF6600"
    }
}
```

3. **Reference your tokens in `.zui.json5` files**:

```json5
$backgroundColor: "${my.custom.accent | color.interactive.primary.background}"
```

The `|` fallback ensures your control still works in themes that don't define
`my.custom.accent`.

4. **Activate your theme** at runtime:

```csharp
appWindow.View.ActiveThemes = ["MyBrandTheme"];
```

## Best Practices

- **Use `|` for custom tokens**: Always provide a fallback to `ZurfurBase` or
  `ZurfurDefault` tokens so third-party users can use your controls without defining all
  your custom tokens.

- **Organize tokens semantically**: Use namespacing like `color.surface.canvas`,
  `spacing.horizontal.medium`, `font.size.base.300`.

- **Leverage `IMergable` for composability**: Properties like `Thickness`, `Font`,
  `SizeRequest` support partial definitions that merge together, enabling flexible token
  reuse.

- **State-based theming**: Use conditional expressions (`isPointerOver ? ...`) for
  interactive states rather than hard-coding them in code-behind.

- **Set themes at the appropriate level**: Application-wide themes go on
  `AppWindow.View.ActiveThemes`, but you can override at any level for special panels or
  dialogs.

- **Theme inheritance**: Child views automatically inherit themes from parent views, so
  you rarely need to set themes more than once.

## Example: Theme Switcher

```csharp
void ThemeComboBox_PropertyChanged(object? sender, PropertyChangedEventArgs e)
{
    if (e.PropertyName != "SelectedIndex")
        return;

    var appWindow = View.AppWindow;
    if (appWindow == null)
        return;

    switch (_themeComboBox.DataContext.SelectedIndex)
    {
        case 0: appWindow.View.ActiveThemes = ["ZurfurDefault"]; break;
        case 1: appWindow.View.ActiveThemes = ["ZurfurDefaultDark"]; break;
        case 2: appWindow.View.ActiveThemes = ["ZurfurCherry"]; break;
        case 3: appWindow.View.ActiveThemes = ["ZurfurCherryDark"]; break;
    }
}
```

## Summary

The ZurfurGui theme system provides:

- **Centralized design tokens** in `.zth.json5` files (with `.zth.json` retained for compatibility)
- **Flexible activation** via `View.ActiveThemes` at any hierarchy level
- **Graceful fallbacks** using `|` for third-party control compatibility
- **State-driven styling** with conditional expressions
- **Composable properties** through `IMergable` and token merging
- **Automatic resolution** that walks the view hierarchy and falls back to base themes

This system replaces the older style sheet system with a simpler, more powerful
token-based approach.

