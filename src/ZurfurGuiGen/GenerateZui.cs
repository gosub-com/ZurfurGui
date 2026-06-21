using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;

namespace ZurfurGuiGen;

using FileInfo = ZurfurGuiGen.ZuiTypes.FileInfo;

[Generator]
public class GenerateZui : IIncrementalGenerator
{


    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var syntaxTrees = context.CompilationProvider.Select((compilation, _) => compilation.SyntaxTrees.ToArray());


        // Collect zuiData for all ZUI.JSON files
        var zuiData = context.AdditionalTextsProvider
            .Where(file => file.Path.EndsWith(".zui.json", StringComparison.OrdinalIgnoreCase))
            .Combine(syntaxTrees)
            .Select((combined, cancellationToken) =>
        {
            var text = combined.Left;
            var syntax = combined.Right;
            return ZuiInput.CollectJsonFiles(text, syntax, cancellationToken);
        });

        // Collect zssData for all ZSS.JSON files
        var zssData = context.AdditionalTextsProvider
            .Where(file => file.Path.EndsWith(".zss.json", StringComparison.OrdinalIgnoreCase))
            .Combine(syntaxTrees)
            .Select((combined, cancellationToken) =>
            {
                var text = combined.Left;
                var syntax = combined.Right;
                return ZuiInput.CollectJsonFiles(text, syntax, cancellationToken);
            });

        // Collect zthData for all ZTH.JSON files (theme tokens)
        var zthData = context.AdditionalTextsProvider
            .Where(file => file.Path.EndsWith(".zth.json", StringComparison.OrdinalIgnoreCase))
            .Combine(syntaxTrees)
            .Select((combined, cancellationToken) =>
            {
                var text = combined.Left;
                var syntax = combined.Right;
                return ZuiInput.CollectJsonFiles(text, syntax, cancellationToken);
            });

        // Generate controller classes (combined with CompilationProvider so inherited bindings
        // can be resolved from referenced assemblies when .implements targets a DLL control)
        context.RegisterSourceOutput(zuiData.Collect().Combine(context.CompilationProvider),
            (spc, pair) => GenerateControllerClasses(spc, pair.Left, pair.Right));

        // Generate the ZurfurMain partial class in each project
        context.RegisterSourceOutput(zuiData.Collect().Combine(zssData.Collect()).Combine(zthData.Collect())
            .Combine(context.CompilationProvider), (sourceProductionContext, quad) =>
        {
            var collectedZuiData = quad.Left.Left.Left;
            var collectedZssData = quad.Left.Left.Right;
            var collectedZthData = quad.Left.Right;
            var compilation = quad.Right;
            ZuiEmitMain.GenerateZurfurMain(sourceProductionContext, compilation, collectedZuiData, collectedZssData, collectedZthData);
        });
    }

    /// <summary>
    /// Collect FileInfo from a .json file.
    /// If a corresponding .cs file exists, collect the namespace from it.
    /// </summary>
    static void GenerateControllerClasses(SourceProductionContext spc, ImmutableArray<FileInfo> zuiData, Compilation compilation)
    {
        // Build a lookup from controller name → full FileInfo for same-project controls.
        var infoByController = zuiData
            .Where(d => d.Diagnostic == null && d.ControllerName != "")
            .ToDictionary(d => d.ControllerName, d => d);

        foreach (var data in zuiData)
        {
            // Should have valid data here
            var errorLocation = Location.Create(data?.Path ?? "(unknown path)", new(), new());
            if (data == null)
            {
                var diagnostic = ZuiDiagnostics.GetDiagnostic(errorLocation, "ZUI003", "Internal Error",
                    $"An internal error occurred during code generation for '{data?.Path}'");
                spc.ReportDiagnostic(diagnostic);
                continue;
            }

            // Report any diagnostics collected during data gathering
            if (data.Diagnostic != null)
            {
                spc.ReportDiagnostic(data.Diagnostic);
                continue;
            }

            try
            {
                // Resolve inherited bindings when .implements is set.
                List<ZuiTypes.DataBinding>? inheritedBindings = null;
                string? implementsNamespace = null;
                if (data.Implements != "")
                {
                    // 1) Same-project: look up the constraint control's FileInfo directly.
                    if (infoByController.TryGetValue(data.Implements, out var fromSourceInfo))
                    {
                        inheritedBindings = fromSourceInfo.Bindings;
                        implementsNamespace = fromSourceInfo.Namespace;
                    }
                    else
                    {
                        // 2) Referenced assembly: synthesize DataBinding objects from the compiled
                        //    I{Implements}Data interface members.
                        //    GetSymbolsWithName only searches the current project's source, so we
                        //    must also walk referenced assembly symbols to find the type.
                        var targetTypeName = $"I{data.Implements}Data";
                        var ifaceSymbol = FindTypeInCompilation(compilation, targetTypeName);
                        if (ifaceSymbol != null)
                        {
                            implementsNamespace = ifaceSymbol.ContainingNamespace?.ToDisplayString();
                            // Limitation: only simple scalar "new" bindings are supported here.
                            // Collection properties (ObservableCollection<>) are detected and rejected
                            // with ZUI007 because IsCollection/IsTypeParam cannot be reconstructed
                            // from metadata alone. Forwarding binds and future bind kinds are also
                            // unsupported — constraint interfaces should only contain scalar properties.
                            var props = ifaceSymbol.GetMembers().OfType<IPropertySymbol>().Where(p => !p.IsStatic).ToList();
                            var synthesized = new List<ZuiTypes.DataBinding>();
                            var hasError = false;
                            foreach (var p in props)
                            {
                                var fullType = p.Type.ToDisplayString();
                                if (fullType.Contains("ObservableCollection"))
                                {
                                    spc.ReportDiagnostic(ZuiDiagnostics.GetDiagnostic(errorLocation,
                                        "ZUI007", "ZUI Implements Cross-Assembly Collection Not Supported",
                                        $"Property '{p.Name}' in 'I{data.Implements}Data' uses ObservableCollection. "
                                        + "Cross-assembly '.implements' does not support collection properties. "
                                        + "Declare it explicitly in '.data' instead."));
                                    hasError = true;
                                }
                                else
                                {
                                    var name = ZuiEmit.ToCamelCase(p.Name);
                                    var pascalName = ZuiEmit.ToPascalCase(name);
                                    synthesized.Add(new ZuiTypes.DataBinding
                                    {
                                        Name = name,
                                        BaseType = p.Type.WithNullableAnnotation(Microsoft.CodeAnalysis.NullableAnnotation.NotAnnotated).ToDisplayString(),
                                        IsNullable = p.NullableAnnotation == Microsoft.CodeAnalysis.NullableAnnotation.Annotated,
                                        Bind = "new",
                                        Comment = "",
                                        PascalName = pascalName,
                                        PropertyKeyName = pascalName + "Property",
                                    });
                                }
                            }
                            if (!hasError)
                                inheritedBindings = synthesized;
                            else
                                continue;
                        }
                        else
                        {
                            // 3) Not found anywhere — emit a compile-time error.
                            spc.ReportDiagnostic(ZuiDiagnostics.GetDiagnostic(errorLocation,
                                "ZUI005", "ZUI Implements Not Found",
                                $"'.implements': \"{data.Implements}\" — constraint interface 'I{data.Implements}Data' "
                                + $"was not found in source or referenced assemblies."));
                            continue;
                        }
                    }

                    // Check for duplicate property names between .data and .implements — error ZUI006.
                    var inheritedNames = new HashSet<string>(inheritedBindings.Select(b => b.Name), StringComparer.OrdinalIgnoreCase);
                    foreach (var binding in data.Bindings)
                    {
                        if (inheritedNames.Contains(binding.Name))
                        {
                            spc.ReportDiagnostic(ZuiDiagnostics.GetDiagnostic(errorLocation,
                                "ZUI006", "ZUI Duplicate Inherited Property",
                                $"Property '{binding.Name}' in '.data' is already declared by '.implements': \"{data.Implements}\". "
                                + $"Remove it from '.data' — it is inherited automatically."));
                        }
                    }
                }

                // Generate the source code for the control
                var source = ZuiEmitController.GenerateControllerClassSource(data, inheritedBindings, implementsNamespace);
                if (source != "")
                    spc.AddSource($"{data.FileName}.Control.g.cs", SourceText.From(source, Encoding.UTF8));

                // Generate the source code for the contract (IControllerNameData) and
                // constraint (IControllerName) interfaces — both go into the same file.
                // Pass the inherited binding names so they are not re-declared in the sub-interface.
                var inheritedNames2 = inheritedBindings != null
                    ? (IEnumerable<string>)inheritedBindings.Select(b => b.Name)
                    : null;
                var contractSource = ZuiEmitContract.GenerateContractInterfaceSource(data, inheritedNames2);
                if (contractSource != "")
                    spc.AddSource($"{data.FileName}.Contract.g.cs", SourceText.From(contractSource, Encoding.UTF8));

                // Generate the source code for the data implementation (partial if *Data.cs exists)
                var dataImplSource = ZuiEmitData.GenerateDataImplementationSource(data, inheritedBindings);
                if (dataImplSource != "")
                    spc.AddSource($"{data.FileName}.Data.g.cs", SourceText.From(dataImplSource, Encoding.UTF8));
            }
            catch (Exception ex)
            {
                // Report a diagnostic error if there is an exception during code generation
                var diagnostic = ZuiDiagnostics.GetDiagnostic(errorLocation, "ZUI004", "ZUI Code Generation Error",
                    $"Error while generating code from '{Path.GetFileName(data.Path)}': {ex.Message}");
                spc.ReportDiagnostic(diagnostic);
            }
        }

    }

    /// <summary>
    /// Searches the compilation and all referenced assemblies for a named type.
    /// GetSymbolsWithName only covers source symbols in the current project, so we
    /// must walk referenced assembly global namespaces to find types compiled into DLLs.
    /// </summary>
    static INamedTypeSymbol? FindTypeInCompilation(Compilation compilation, string typeName)
    {
        // First check source symbols in the current project.
        var fromSource = compilation.GetSymbolsWithName(typeName, SymbolFilter.Type)
            .OfType<INamedTypeSymbol>()
            .FirstOrDefault();
        if (fromSource != null)
            return fromSource;

        // Walk all referenced assembly namespaces recursively.
        foreach (var reference in compilation.References)
        {
            var assemblySymbol = compilation.GetAssemblyOrModuleSymbol(reference) as IAssemblySymbol;
            if (assemblySymbol == null)
                continue;
            var found = FindTypeInNamespace(assemblySymbol.GlobalNamespace, typeName);
            if (found != null)
                return found;
        }

        return null;
    }

    static INamedTypeSymbol? FindTypeInNamespace(INamespaceSymbol ns, string typeName)
    {
        foreach (var type in ns.GetTypeMembers(typeName))
            return type;
        foreach (var child in ns.GetNamespaceMembers())
        {
            var found = FindTypeInNamespace(child, typeName);
            if (found != null)
                return found;
        }
        return null;
    }

}
