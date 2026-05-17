namespace MS.Internal;

public static class Invariant
{
    public static bool Strict => false;

    public static void Assert(bool condition)
    {
    }

    public static void Assert(bool condition, string message)
    {
    }
}
