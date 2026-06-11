namespace System.Windows;

// Flat designer-metadata shim following the AttachedPropertyBrowsableForTypeAttribute
// pattern. WPF's IsBrowsable tree walk only matters to designers and depends on
// internal framework-parent traversal that has no Uno equivalent.
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class AttachedPropertyBrowsableForChildrenAttribute : Attribute
{
    public bool IncludeDescendants { get; set; }

    public override bool Equals(object? obj)
        => obj is AttachedPropertyBrowsableForChildrenAttribute other &&
           IncludeDescendants == other.IncludeDescendants;

    public override int GetHashCode() => IncludeDescendants.GetHashCode();
}
