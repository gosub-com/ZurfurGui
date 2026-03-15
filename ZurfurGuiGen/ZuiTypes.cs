using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace ZurfurGuiGen;

internal static class ZuiTypes
{
    internal class DataBinding
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
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
        /// Full namespace + controller class name
        /// </summary>
        public string NamespaceFileName => $"{Namespace}.{ControllerName}";
    }
}
