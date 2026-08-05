namespace ZurfurGui.Base;

/// <summary>
/// Font metrics for accurate text positioning and layout.
/// All values are in pixels and scale linearly with font size.
/// </summary>
public readonly record struct FontMetrics(
    /// <summary>
    /// Distance from baseline to the top of the tallest glyph (positive value).
    /// Typically 75-80% of font size.
    /// </summary>
    double Ascent,

    /// <summary>
    /// Distance from baseline to the bottom of the lowest glyph (positive value).
    /// Typically 20-25% of font size.
    /// </summary>
    double Descent,

    /// <summary>
    /// Recommended distance between baselines of consecutive lines.
    /// Typically Ascent + Descent + LineGap (usually 1.15-1.2x font size).
    /// </summary>
    double LineHeight
);
