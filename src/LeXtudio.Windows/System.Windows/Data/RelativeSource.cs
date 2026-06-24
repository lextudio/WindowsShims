namespace System.Windows.Data;

public enum RelativeSourceMode
{
    PreviousData,
    TemplatedParent,
    Self,
    FindAncestor,
}

public class RelativeSource
{
    public static RelativeSource PreviousData { get; } = new(RelativeSourceMode.PreviousData);

    public static RelativeSource TemplatedParent { get; } = new(RelativeSourceMode.TemplatedParent);

    public static RelativeSource Self { get; } = new(RelativeSourceMode.Self);

    public RelativeSource()
    {
        Mode = RelativeSourceMode.FindAncestor;
    }

    public RelativeSource(RelativeSourceMode mode)
    {
        Mode = mode;
    }

    public RelativeSource(RelativeSourceMode mode, Type ancestorType, int ancestorLevel)
    {
        Mode = mode;
        AncestorType = ancestorType;
        AncestorLevel = ancestorLevel;
    }

    public RelativeSourceMode Mode { get; set; }

    public Type? AncestorType { get; set; }

    public int AncestorLevel { get; set; } = 1;
}
