namespace MS.Internal;

// Minimal shim for WPF's MS.Internal.DoubleUtil.
// WPF uses floating-point epsilon comparisons; for layout purposes exact comparisons suffice here.
internal static class DoubleUtil
{
    internal static bool GreaterThan(double value1, double value2) => value1 > value2;
    internal static bool LessThan(double value1, double value2) => value1 < value2;
    internal static bool GreaterThanOrClose(double value1, double value2) => value1 >= value2;
    internal static bool LessThanOrClose(double value1, double value2) => value1 <= value2;
    internal static bool AreClose(double value1, double value2) => value1 == value2;
    internal static bool IsZero(double value) => value == 0.0;
    internal static bool IsNaN(double value) => double.IsNaN(value);
}
