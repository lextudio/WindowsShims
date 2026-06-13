namespace MS.Internal;

internal static class DoubleUtil
{
    private const double Epsilon = 2.2204460492503131e-016;

    internal static bool AreClose(double value1, double value2)
    {
        if (value1 == value2)
        {
            return true;
        }

        double tolerance = ((Math.Abs(value1) + Math.Abs(value2)) + 10.0) * Epsilon;
        double delta = value1 - value2;
        return -tolerance < delta && tolerance > delta;
    }

    internal static bool IsOne(double value) => Math.Abs(value - 1.0) < 10.0 * Epsilon;

    internal static bool LessThan(double value1, double value2)
        => value1 < value2 && !AreClose(value1, value2);

    internal static bool AreClose(Windows.Foundation.Point point1, Windows.Foundation.Point point2)
        => AreClose(point1.X, point2.X) && AreClose(point1.Y, point2.Y);
}

internal readonly struct PixelUnit
{
    private PixelUnit(string name, double factor)
    {
        Name = name;
        Factor = factor;
    }

    internal string Name { get; }

    internal double Factor { get; }

    internal static bool TryParsePixelPerInch(ReadOnlySpan<char> value, out PixelUnit pixelUnit)
        => TryParse(value, "in", 96.0, out pixelUnit);

    internal static bool TryParsePixelPerCentimeter(ReadOnlySpan<char> value, out PixelUnit pixelUnit)
        => TryParse(value, "cm", 96.0 / 2.54, out pixelUnit);

    internal static bool TryParsePixelPerPoint(ReadOnlySpan<char> value, out PixelUnit pixelUnit)
        => TryParse(value, "pt", 96.0 / 72.0, out pixelUnit);

    private static bool TryParse(ReadOnlySpan<char> value, string unitName, double factor, out PixelUnit pixelUnit)
    {
        if (value.TrimEnd().EndsWith(unitName, StringComparison.OrdinalIgnoreCase))
        {
            pixelUnit = new PixelUnit(unitName, factor);
            return true;
        }

        pixelUnit = default;
        return false;
    }
}
