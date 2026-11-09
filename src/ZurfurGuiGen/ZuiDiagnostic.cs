using Microsoft.CodeAnalysis;

namespace ZurfurGuiGen;

internal static class ZuiDiagnostics
{
    /// <summary>
    /// Creates a diagnostic for a nullable source bound to a non-nullable data property.
    /// </summary>
    internal static Diagnostic GetNullableBindingDiagnostic(
        Location location,
        string destinationFile,
        string destinationProperty,
        string destinationType,
        string bindPath,
        string sourceController,
        string sourceProperty,
        string sourceType)
    {
        var message = $"Data property '$data.{destinationProperty}' in '{destinationFile}' is "
            + $"non-nullable ('{destinationType}'), but binding '{bindPath}' reads nullable "
            + $"property '{sourceController}.{sourceProperty}' ('{sourceType}'). This could "
            + "assign null to a non-nullable property. Declare the destination type as "
            + $"'?{destinationType}' or bind to a non-nullable source.";
        return GetDiagnostic(location, "ZUI006", "Nullable Binding Error", message);
    }

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
