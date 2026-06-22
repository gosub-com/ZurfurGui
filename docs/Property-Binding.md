# Property Binding

ZurfurGUI's architecture is built upon a unified property system that serves as the bridge
between the data model (D), the view (V), and the style engine. It's designed to be
flexible, allowing properties to be simple data, stylable values, or a combination of both.
This model is the foundation for **Data-Driven Styling**, where a control's appearance can be
directly influenced by the application's data and state.

## The .data Section

Controls declare the properties they expose in the `.data` section of their `.zui.json` file.
The source generator uses this section to create the strongly-typed data context interface
(`IData`) and a concrete data class (`Data`), as well as generate `PropertyKey` fields in the
controller class.

Each entry in `.data` specifies the property's name, its `type`, and its binding behavior via
the `bind` keyword, along with optional fields like `default` and `flags`.

### .data Property Fields

The following table describes the fields that can be used in each property definition within the `.data` section:

| Field | Required | Description | Example |
|-------|----------|-------------|---------|
| `type` | Yes | The type of the property. Prefix with `?` for nullable types, `[]` for collections. | `Color`, `?string`, `[]Item` |
| `bind` | Yes | Specifies how the property participates in data binding and styling. See **Property Categories** below. | `data`, `styledData`, `styledOnly`, `attached`, `_childName.propertyName` |
| `default` | No | The expression for the default value. If omitted, `new()` is used for value types, `null` for nullable types. | `100`, `Orientation.horizontal`, `new Color(128, 128, 128)` |
| `flags` | No | Comma-separated list of ViewFlags that control when the view should be invalidated. If omitted, `ViewFlags.none` is used. | `draw`, `measure`, `styleThis`, `styleDown` |

For example, `ScrollBar.zui.json` defines its properties like this:
```jsonc
// ...
".data": {
	"value": {
		"type": "double",
		"bind": "data"
	},
	"orientation": {
		"type": "Orientation",
		"bind": "styledData",
		"default": "Orientation.horizontal"
	},
	"thumbColor": {
		"type": "Color",
		"bind": "styledOnly",
		"default": "new Color(128, 128, 128)",
		"flags": "draw"
	}
}
// ...
```

## Property Categories

There are five kinds of properties in the ZurfurGUI system. Choosing the right kind is key to
creating flexible and maintainable controls.

### 1. Data

*   **Description:** A pure data property that holds the state or value of a control. It is not
	influenced by the style system.
*   **Mechanism:** Uses `"bind": "data"`.
*   **Generated Code:** Creates a property in the `IData` interface and concrete `Data` class,
	plus a `PropertyKey` field in the controller (without ViewFlags by default).
*   **When to Use:** For the essential value of a control that application logic needs to read
	and write (e.g., `ScrollBar.Value`, `CheckBox.IsChecked`). This is for non-visual state
	that is fundamental to the control's function.

### 2. StyledData

*   **Description:** The foundation of Data-Driven Styling. This is a property that is exposed
	on the data context but also participates in the style system. If the data context
	provides a value, it is used; if the value is `null`, the system falls back to the value
	provided by the active stylesheets.
*   **Mechanism:** Uses `"bind": "styledData"`.
*   **Generated Code:** Creates a nullable property in the `IData` interface and concrete `Data`
	class, plus a `PropertyKey` field in the controller. The controller merges data and style
	values at runtime.
*   **When to Use:** For any visual property that you have a common and compelling reason for
	application logic to control dynamically. Examples include changing a `TextView`'s color
	based on a validation error or making text bold based on a selection state.

### 3. Styled (Style-Only)

*   **Description:** A property that defines the look-and-feel of a control but is not exposed
	on the data context. It can only be configured via stylesheets or by imperative code
	(`view.SetProperty`).
*   **Mechanism:** Uses `"bind": "styledOnly"` in the `.data` section.
*   **Generated Code:** Creates only a `PropertyKey` field in the controller class. Does **not**
	generate any property in the `IData` interface or `Data` class, and does not participate
	in observable change tracking.
*   **When to Use:** For theme-related visual properties that are not typically driven by
	application state. Examples include the hover color of a button, the thickness of a
	scrollbar, or the font used for headers in a markdown viewer. Commonly used with `flags`
	to specify `ViewFlags.Draw` or `ViewFlags.Measure`.

### 4. Forwarded

*   **Description:** A "Forwarded" property is a convenience property on a composite control
	that delegates its value directly to a property on one of its child controls.
*   **Mechanism:** Uses the `"bind": "_childName.propertyName"` syntax. For example, the
	`CheckBox` control forwards its `text` property to its internal `_checkText` `TextView`
	child with `"bind": "_checkText.text"`.
*   **Generated Code:** Creates a property in the `IData` interface and `Data` class that
	forwards get/set calls to the specified child control's data context property.
*   **When to Use:** Use this to simplify the API of a composite control, allowing users to set
	a child's property without needing to access the child directly.

### 5. Attached

*   **Description:** A property defined by one control but designed to be set on other
	controls. It allows a parent container to attach layout information or other metadata to
	its children.
*   **Mechanism:** An attached `PropertyKey` is defined in C# code. It is not part of any
	control's specific data context.
*   **When to Use:** For properties that a parent panel needs to manage its children, such as
	`Grid.Row`, `Grid.Column`, or a custom panel's `Dock` property.
*   **Global Properties Defined on Panel:** The `Panel` control defines a special set of
	attached properties that act as global properties for all controls. In ZUI JSON, these do
	not need the "Panel." prefix. For example, you can use `.isVisible` directly. Other
	examples include `.align`, `.margin`, `.backgroundColor`, and `.borderWidth`.

