namespace ZurfurGui.Property;

/// <summary>
/// This is like a css file
/// </summary>
public record class StyleSheet
{
    /// <summary>
    /// Unique name of style sheet, or "" if not named
    /// </summary>
    public string Name { get; init; } = "";
    public Properties[] Styles { get; init; } = [];
}


