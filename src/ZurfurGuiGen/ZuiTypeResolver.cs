using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using ZurfurGuiGen.ZuiTypes;

namespace ZurfurGuiGen;

/// <summary>
/// Resolves the bare class name of a control (e.g. "ComboBox" or "ComboBoxItemText") used in a
/// closed generic usage (e.g. "ComboBox&lt;ComboBoxItemText&gt;") to its fully qualified C# name,
/// so the generator can emit a statically visible closed generic type.  The compiler (and the
/// AOT/trimming tools) must see the closed type so its constructor is kept and compiled.
/// Controls generated in the current project are found from the ZUI files.  Controls declared in
/// referenced assemblies (e.g. the ZurfurGui library or third-party libraries) are found by
/// searching the public types of the referenced assemblies.
/// </summary>
internal class ZuiTypeResolver
{
    readonly List<ZuiFileInfo> _controls;
    readonly Compilation _compilation;
    readonly Dictionary<string, List<string>> _cache = new();

    /// <summary>
    /// Creates a resolver for the controls generated in this project and the types visible
    /// to the given compilation.
    /// </summary>
    public ZuiTypeResolver(List<ZuiFileInfo> controls, Compilation compilation)
    {
        _controls = controls;
        _compilation = compilation;
    }

    /// <summary>
    /// Finds the namespace qualified name (without "global::") of the control class with the given
    /// bare name and generic arity (0 for non-generic).  Returns null and sets
    /// <paramref name="error"/> when the name is not found or is ambiguous.
    /// </summary>
    public string? Resolve(string bareName, int arity, out string error)
    {
        var matches = FindAll(bareName, arity);
        error = "";
        if (matches.Count == 1)
            return matches[0];

        error = matches.Count == 0
            ? $"Control '{bareName}' was not found in this project or its referenced libraries."
            : $"Control name '{bareName}' is ambiguous: {string.Join(", ", matches)}.";
        return null;
    }

    /// <summary>
    /// Finds all distinct matches, first from this project's ZUI files, then (only when there
    /// are none) from the referenced assemblies.
    /// </summary>
    List<string> FindAll(string bareName, int arity)
    {
        var key = $"{bareName}`{arity}";
        if (_cache.TryGetValue(key, out var cached))
            return cached;

        var local = _controls
            .Where(c => c.ControllerName == bareName && (c.TypeParam != "" ? 1 : 0) == arity)
            .Select(c => c.NamespaceFileName)
            .Distinct()
            .ToList();

        var result = local.Count != 0 ? local : FindInReferences(bareName, arity);
        _cache[key] = result;
        return result;
    }

    /// <summary>
    /// Searches the referenced assemblies for accessible top level types with the given name.
    /// </summary>
    List<string> FindInReferences(string bareName, int arity)
    {
        var result = new List<string>();
        foreach (var assembly in _compilation.SourceModule.ReferencedAssemblySymbols)
            Search(assembly.GlobalNamespace, bareName, arity, result);
        return result.Distinct().ToList();
    }

    /// <summary>
    /// Recursively searches a namespace for types matching the name and arity.
    /// </summary>
    void Search(INamespaceSymbol ns, string bareName, int arity, List<string> result)
    {
        foreach (var type in ns.GetTypeMembers(bareName, arity))
        {
            if (_compilation.IsSymbolAccessibleWithin(type, _compilation.Assembly))
            {
                var typeNamespace = type.ContainingNamespace.ToDisplayString();
                result.Add(typeNamespace == "" ? type.Name : $"{typeNamespace}.{type.Name}");
            }
        }
        foreach (var child in ns.GetNamespaceMembers())
            Search(child, bareName, arity, result);
    }
}
