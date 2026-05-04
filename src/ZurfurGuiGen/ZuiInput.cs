using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace ZurfurGuiGen;

internal static class ZuiInput
{
    /// <summary>
    /// Collect FileInfo from a .json file.
    /// If a corresponding .cs file exists, collect the namespace from it.
    /// </summary>
    internal static ZuiTypes.FileInfo CollectJsonFiles(AdditionalText text, SyntaxTree[] syntax, CancellationToken cancellationToken)
    {
        var zuiPath = text.Path;
        var jsonNameWithoutJsonExt = Path.GetFileNameWithoutExtension(zuiPath); // removes .json
        var jsonNameWithoutAnyExt = Path.GetFileNameWithoutExtension(jsonNameWithoutJsonExt); // removes .zui or .zss

        // Parse the json file (errors are collected into diagnostic)
        Diagnostic? diagnostic = null;
        Dictionary<string, object?> jsonDocument = new();
        var controllerName = "";
        var nameSpace = "";
        var fileNameOnly = Path.GetFileNameWithoutExtension(jsonNameWithoutJsonExt); // removes .zui or .zss
        var userSuppliedControllerClass = false;
        var userSuppliedDataClass = false;
        var use = new List<string>();
        var dataBindings = new List<ZuiTypes.DataBinding>();
        try
        {
            // Parse the JSON content, find the controller or style name
            jsonDocument = Json.Parse(text.GetText(cancellationToken)?.ToString() ?? "");

            if (Path.GetExtension(jsonNameWithoutJsonExt).ToLower() == ".zss")
            {
                controllerName = GetStyleName(fileNameOnly, jsonDocument);
            }
            else
            {
                nameSpace = GetJsonValue(jsonDocument, ".namespace");

                use = GetUsingLines(jsonDocument);

                // Try to find the .cs file with the same name as the JSON file (code-behind)
                var csTree = syntax.FirstOrDefault(tree =>
                {
                    var csFileName = Path.GetFileNameWithoutExtension(tree.FilePath);
                    return csFileName == $"{jsonNameWithoutAnyExt}.Control";
                });

                var csDataTree = syntax.FirstOrDefault(tree =>
                {
                    var csFileName = Path.GetFileNameWithoutExtension(tree.FilePath);
                    return csFileName == $"{jsonNameWithoutAnyExt}.Data";
                });

                var csFileNamespace = GetNameSpace(csTree) ?? "";
                userSuppliedControllerClass = csFileNamespace != "";

                var csDataFileNamespace = GetNameSpace(csDataTree) ?? "";
                userSuppliedDataClass = csDataFileNamespace != "";

                // Verify the .cs file namespace (if any) matches the JSON namespace
                if (csFileNamespace != "" && csFileNamespace != nameSpace)
                    throw new Exception($"The JSON '.namespace' must match the namespace in {fileNameOnly}.Control.cs, "
                        + $"but is '{nameSpace}' instead of '{csFileNamespace}'");

                // Verify the *Data.cs file namespace (if any) matches the JSON namespace
                if (csDataFileNamespace != "" && csDataFileNamespace != nameSpace)
                    throw new Exception($"The JSON '.namespace' must match the namespace in {fileNameOnly}.Data.cs, "
                        + $"but is '{nameSpace}' instead of '{csDataFileNamespace}'");

                controllerName = GetControllerName(fileNameOnly, jsonDocument);

                dataBindings = ZuiSchema.GetDataBindings(jsonDocument)
                    .OrderBy(p => p.Name)
                    .ToList();
            }
        }
        catch (LocationException lex)
        {
            // Report a diagnostic error if there is a LocationException while parsing
            var errorLocation = Location.Create(zuiPath, new TextSpan(0, 0),
                new LinePositionSpan(new LinePosition(lex.Line, lex.Column), new LinePosition(lex.Line, lex.Column)));
            diagnostic = ZuiDiagnostics.GetDiagnostic(errorLocation,
                "ZUI001", "ZUI JSON Parsing Error",
                $"Error while parsing JSON '{Path.GetFileName(zuiPath)}': {lex.Message}");
        }
        catch (Exception ex)
        {
            // Report a diagnostic error if there is an exception while parsing
            var errorLocationTop = Location.Create(zuiPath,
                new TextSpan(0, 0), new LinePositionSpan(new LinePosition(0, 0), new LinePosition(0, 0)));
            diagnostic = ZuiDiagnostics.GetDiagnostic(errorLocationTop, "ZUI002", "ZUI Code Generation Error",
                $"Error while generating code from '{Path.GetFileName(zuiPath)}': {ex.Message}");
        }

        return new ZuiTypes.FileInfo
        {
            Path = zuiPath,
            FileName = fileNameOnly,
            Diagnostic = diagnostic,
            JsonDocument = jsonDocument,
            ControllerName = controllerName,
            Namespace = nameSpace,
            Use = use,
            Bindings = dataBindings,
            UserSuppliedControllerClass = userSuppliedControllerClass,
            UserSuppliedDataClass = userSuppliedDataClass
        };
    }

    /// <summary>
    /// Retrieve optional extra using directive lines from ZUI JSON `.use`.
    /// The value may be a string or an array of strings.
    /// </summary>
    static List<string> GetUsingLines(Dictionary<string, object?> jsonDocument)
    {
        if (!jsonDocument.TryGetValue(".use", out var useObj) || useObj == null)
            return new List<string>();

        if (useObj is List<object?> list)
        {
            var result = new List<string>();
            foreach (var item in list)
            {
                if (item is string s && !string.IsNullOrWhiteSpace(s))
                    result.Add(s);
                else
                    throw new Exception("The JSON '.use' array must contain only strings");
            }
            return result;
        }

        throw new Exception("The JSON '.use' key must be an array of strings");
    }

    /// <summary>
    /// Find and validate the controller name from the json.
    /// Throws an exception if invalid.
    /// </summary>
    static string GetControllerName(string fileName, Dictionary<string, object?> jsonDocument)
    {
        // Verify controller key is present
        string controllerName = GetJsonValue(jsonDocument, ".controller");

        if (controllerName != fileName)
            throw new Exception($"The JSON '.controller' key must match the class and file name '{fileName}', but is '{controllerName}' instead");

        return controllerName;
    }

    /// <summary>
    /// Retrieve a non-empty string value from the JSON document.
    /// Throws an exception if the key doesn't exist, is not a string, or is empty.
    /// </summary>
    static string GetJsonValue(Dictionary<string, object?> jsonDocument, string key)
    {
        if (!jsonDocument.TryGetValue(key, out var controllerKey))
            throw new Exception($"Top level JSON must contain a '{key}' key");
        if (controllerKey is not string controllerName || string.IsNullOrWhiteSpace(controllerName))
            throw new Exception($"The JSON '{key}' key must be a non-empty string");
        return controllerName;
    }

    static string GetStyleName(string fileName, Dictionary<string, object?> jsonDocument)
    {
        // Retrieve and validate the style name
        if (!jsonDocument.TryGetValue("name", out var styleJsonObject))
            throw new Exception("Top level JSON must contain a 'name' key");
        if (styleJsonObject is not string styleName || string.IsNullOrWhiteSpace(styleName))
            throw new Exception("The JSON 'Name' key must be a non-empty string");
        if (styleName != fileName)
            throw new Exception($"The JSON 'Name' key must match the file name '{fileName}', but is '{styleName}' instead");
        return styleName;
    }

    static string? GetNameSpace(SyntaxTree? csTree)
    {
        if (csTree == null)
            return null;

        var root = csTree.GetRoot();

        // Try to get the namespace from the namespace declaration
        var namespaceNode = root.DescendantNodes()
            .OfType<NamespaceDeclarationSyntax>()
            .FirstOrDefault();

        if (namespaceNode != null)
        {
            return BuildNamespaceName(namespaceNode);
        }

        // Try file-scoped namespace
        var fileScopedNs = root.DescendantNodes()
            .OfType<FileScopedNamespaceDeclarationSyntax>()
            .FirstOrDefault();

        if (fileScopedNs != null)
        {
            return fileScopedNs.Name.ToString();
        }

        return null;

        // Helper function to recursively build the fully qualified namespace
        static string BuildNamespaceName(SyntaxNode node)
        {
            if (node is NamespaceDeclarationSyntax namespaceNode)
            {
                var parentNamespace = namespaceNode.Parent is NamespaceDeclarationSyntax
                    ? BuildNamespaceName(namespaceNode.Parent)
                    : null;

                return parentNamespace == null
                    ? namespaceNode.Name.ToString()
                    : $"{parentNamespace}.{namespaceNode.Name}";
            }

            return string.Empty;
        }
    }
}
