using ZurfurGui.Base;

namespace ZurfurGui.Property;

public class StyleProperty
{
    public required TextLines Selectors { get; init; } = [];
    public Properties Properties { get; init; } = [];
}

