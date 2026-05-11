using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace ZurfurGuiGen;

internal static class ZuiTypes
{
    internal class DataBinding
    {
        public string Name { get; set; } = "";
        public string Comment { get; set; } = "";
        public string BaseType { get; set; } = "";
        public string NullableType => IsNullable ? $"{BaseType}?" : BaseType;
        public string Bind { get; set; } = "";
        public bool IsNullable;
        /// <summary>True when the type was declared as []Type (an observable collection of items).</summary>
        public bool IsCollection;
        /// <summary>
        /// True when this collection's element type is the file's declared generic type parameter
        /// (e.g. "[]Item" in a file whose .controller is "ComboBox&lt;Item&gt; where Item : ComboBoxItem").
        /// When true, the element type resolves to the constraint's data interface rather than a
        /// concrete data type, keeping the data layer non-generic.
        /// </summary>
        public bool IsTypeParam;
    }

    // NOTE: We can't use record class here because code generators must target netstandard2.0
    internal class FileInfo
    {
        public string Path { get; set; } = "";
        public string FileName { get; set; } = "";

        /// <summary>
        /// If present, any of the data below may be missing or invalid.
        /// </summary>
        public Diagnostic? Diagnostic { get; set; }

        /// <summary>
        /// The parsed JSON document (or empty if parsing failed)
        /// </summary>
        public Dictionary<string, object?> JsonDocument { get; set; } = new();

        /// <summary>
        /// Top-level // comment from the JSON file, or "" if none.
        /// </summary>
        public string Comment { get; set; } = "";

        /// <summary>
        /// The controller or style name value from the JSON document (of "" if missing)
        /// </summary>
        public string ControllerName = "";

        /// <summary>
        /// Namespace (or "" for styles)
        /// </summary>
        public string Namespace { get; set; } = "";

        /// <summary>
        /// Optional extra C# using directives (one per line) to include in generated output.
        /// </summary>
        public List<string> Use { get; set; } = new();

        /// <summary>
        /// Data bindings extracted from the JSON `.data` section.
        /// </summary>
        public List<DataBinding> Bindings { get; set; } = new();

        /// <summary>
        /// Set to true if there is a user supplied controller class
        /// </summary>
        public bool UserSuppliedControllerClass { get; set; }

        /// <summary>
        /// Set to true if there is a user supplied data class (<ViewName>.Data.cs)
        /// </summary>
        public bool UserSuppliedDataClass { get; set; }

        /// <summary>
        /// When set, the generated data class additionally implements I{Implements}Data,
        /// and the generated controller class additionally implements I{Implements}.
        /// Populated from the JSON ".implements" key (e.g. ".implements": "ComboBoxItem").
        /// </summary>
        public string Implements { get; set; } = "";

        /// <summary>
        /// Generic type parameter name parsed from the .controller value
        /// (e.g. "Item" from "ComboBox&lt;Item&gt; where Item : ComboBoxItem").
        /// Empty string when the control is not generic.
        /// </summary>
        public string TypeParam { get; set; } = "";

        /// <summary>
        /// Constraint control name parsed from the .controller where clause
        /// (e.g. "ComboBoxItem" from "ComboBox&lt;Item&gt; where Item : ComboBoxItem").
        /// The generated C# constraint becomes "where Item : IComboBoxItemData".
        /// Empty string when the control is not generic.
        /// </summary>
        public string TypeParamConstraint { get; set; } = "";

        /// <summary>
        /// Full namespace + controller class name
        /// </summary>
        public string NamespaceFileName => $"{Namespace}.{ControllerName}";
    }
}
