# Property Binding

ZurfurGUI's architecture is built upon a unified property system that serves as the bridge
between the data model (D), the view (V), and the style engine. It's designed to be
flexible, allowing properties to be simple data, stylable values, or a combination of both.
This model is the foundation for **Data-Driven Styling**, where a control's appearance can be
directly influenced by the application's data and state.

## The .data Section

Controls declare the properties they expose in the `.data` section of their `.zui.json` file.
The source generator uses this section to create the strongly-typed data context interface
(`IData`) and a concrete data class (`Data`).

Each entry in `.data` specifies the property's name, its `type`, and its binding behavior via
the `bind` keyword.

For example, `TextView.zui.json` defines its core properties like this:
```jsonc
// ...
".data": {
	"text": {
		"type": "?TextLines",
		"bind": "styled"
	},
	"font": {
		"type": "?FontProp",
		"bind": "styled"
	},
	"color": {
		"type": "?Color",
		"bind": "styled"
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
*   **When to Use:** For the essential value of a control that application logic needs to read
	and write (e.g., `ScrollBar.Value`, `CheckBox.IsChecked`). This is for non-visual state
	that is fundamental to the control's function.

### 2. StyledData

*   **Description:** The foundation of Data-Driven Styling. This is a property that is exposed
	on the data context but also participates in the style system. If the data context
	provides a value, it is used; if the value is `null`, the system falls back to the value
	provided by the active stylesheets.
*   **Mechanism:** Uses `"bind": "styled"`.
*   **When to Use:** For any visual property that you have a common and compelling reason for
	application logic to control dynamically. Examples include changing a `TextView`'s color
	based on a validation error or making text bold based on a selection state.

### 3. Styled (Style-Only)

*   **Description:** A property that defines the look-and-feel of a control but is not exposed
	on the data context. It can only be configured via stylesheets or by imperative code
	(`view.SetProperty`).
*   **Mechanism:** A `PropertyKey` is defined in the control's C# code-behind ~~but is **not**
	included in the `.data` section of the `.zui.json` file.~~  TBD: This will change, as it
	will be defined in the ".data" section in the next revision.
*   **When to Use:** For theme-related visual properties that are not typically driven by
	application state. Examples include the hover color of a button, the thickness of a
	scrollbar, or the font used for headers in a markdown viewer.

### 4. Forwarded

*   **Description:** A "Forwarded" property is a convenience property on a composite control
	that delegates its value directly to a property on one of its child controls.
*   **Mechanism:** Uses the `"bind": "_childName.propertyName"` syntax. For example, the
	`CheckBox` control forwards its `text` property to its internal `_checkText` `TextView`
	child with `"bind": "_checkText.text"`.
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

