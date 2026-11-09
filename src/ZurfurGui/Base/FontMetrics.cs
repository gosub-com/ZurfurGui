namespace ZurfurGui.Base;

/// <summary>
/// Font metrics for accurate text positioning and layout.
/// All values are in pixels and scale linearly with font size.
/// </summary>
/// <param name="Ascent">
/// Distance from baseline to the top of the tallest glyph (positive value).
/// Typically 75-80% of font size.
/// </param>
/// <param name="Descent">
/// Distance from baseline to the bottom of the lowest glyph (positive value).
/// Typically 20-25% of font size.
/// </param>
/// <param name="LineHeight">
/// Recommended distance between baselines of consecutive lines.
/// Typically Ascent + Descent + LineGap (usually 1.15-1.2x font size).
/// </param>
public readonly record struct FontMetrics(
    double Ascent,
    double Descent,
    double LineHeight
);
