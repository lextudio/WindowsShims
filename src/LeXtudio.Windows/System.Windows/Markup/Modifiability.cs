namespace System.Windows.Markup
{
    public enum Modifiability
    {
        Modifiable,
        Unmodifiable,
        Inherit,
    }

    public enum Readability
    {
        Unreadable = 0,
        Readable   = 1,
        Inherit    = 2,
    }
}
