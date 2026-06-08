namespace MS.Internal.KnownBoxes;

internal static class BooleanBoxes
{
    internal static readonly object TrueBox = true;
    internal static readonly object FalseBox = false;
    internal static object Box(bool value) => value ? TrueBox : FalseBox;
}
