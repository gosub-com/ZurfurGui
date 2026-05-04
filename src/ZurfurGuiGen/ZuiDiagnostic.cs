using Microsoft.CodeAnalysis;

namespace ZurfurGuiGen;

internal static class ZuiDiagnostics
{
    internal static Diagnostic GetDiagnostic(Location location, string id, string title, string messageFormat)
    {
        var descriptor = new DiagnosticDescriptor(
            id,
            title,
            messageFormat,
            category: "ZurfurGuiGen",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );
        return Diagnostic.Create(descriptor, location);
    }
}
