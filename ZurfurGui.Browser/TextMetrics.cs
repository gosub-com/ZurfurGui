namespace ZurfurGui;

public record class TextMetrics(
    string Text,
    double Width,
    double ActualBoundingBoxLeft,
    double ActualBoundingBoxRight,
    double FontBoundingBoxAscent,
    double FontBoundingBoxDescent,
    double ActualBoundingBoxAscent,
    double ActualBoundingBoxDescent,
    double EmHeightAscent,
    double EmHeightDescent,
    double HangingBaseline,
    double AlphabeticBaseline,
    double IdeographicBaseline)
{
    public override string ToString()
    {
        return $"w:{Width:F2}, abbl:{ActualBoundingBoxLeft:F2}, abbr:{ActualBoundingBoxRight:F2}, fbba:{FontBoundingBoxAscent:F2}, \r\n"
            + $"fbbd:{FontBoundingBoxDescent:F2}, abba:{ActualBoundingBoxAscent:F2}, abbd:{ActualBoundingBoxDescent:F2}, "
            + $"mha:{EmHeightAscent:F2}, mhd:{EmHeightDescent:F2}, hbl:{HangingBaseline:F2}, abl:{AlphabeticBaseline:F2}, ibl:{IdeographicBaseline:F2}";
    }
}
