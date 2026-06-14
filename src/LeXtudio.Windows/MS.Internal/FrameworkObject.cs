namespace MS.Internal;

internal readonly struct FrameworkObject
{
    public FrameworkObject(DependencyObject? dependencyObject)
    {
    }

    public DependencyObject? Parent => null;

    public void ChangeLogicalParent(DependencyObject? newParent)
    {
    }
}
