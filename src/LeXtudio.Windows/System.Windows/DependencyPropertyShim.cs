namespace System.Windows;

public sealed class DependencyPropertyShim
{
    public void OverrideMetadata(Type forType, FrameworkPropertyMetadata metadata)
    {
    }

    // AddOwner: WPF API to register the same property on a different owner type.
    // Shim returns the same instance (semantics simplification - sufficient for compile-time).
    public DependencyPropertyShim AddOwner(Type ownerType) => this;
    public DependencyPropertyShim AddOwner(Type ownerType, FrameworkPropertyMetadata metadata) => this;
}
