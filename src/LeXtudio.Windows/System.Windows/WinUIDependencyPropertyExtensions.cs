namespace System.Windows;

using System.Collections.Concurrent;

/// <summary>Thin wrapper for WPF read-only dependency property key — WinUI has no concept of read-only DPs.</summary>
public sealed class DependencyPropertyKey
{
    internal DependencyPropertyKey(Microsoft.UI.Xaml.DependencyProperty dp) => DependencyProperty = dp;
    public Microsoft.UI.Xaml.DependencyProperty DependencyProperty { get; }

    public void OverrideMetadata(Type forType, Microsoft.UI.Xaml.PropertyMetadata typeMetadata)
        => DependencyProperty.OverrideMetadata(forType, typeMetadata);

    public void OverrideMetadata(Type forType, FrameworkPropertyMetadata typeMetadata)
        => DependencyProperty.OverrideMetadata(forType, typeMetadata);
}

/// <summary>
/// WPF compatibility extension methods on Microsoft.UI.Xaml.DependencyProperty.
/// AddOwner is a WPF API for sharing a DependencyProperty across multiple owner types;
/// WinUI/Uno doesn't natively support this so we return the same instance.
/// Used by WPF source files like TableColumn that call Panel.BackgroundProperty.AddOwner(...).
/// </summary>
public static class WinUIDependencyPropertyExtensions
{
    private static readonly ConcurrentDictionary<Microsoft.UI.Xaml.DependencyProperty, Type> PropertyTypes = new();

    private static Microsoft.UI.Xaml.DependencyProperty TrackPropertyType(Microsoft.UI.Xaml.DependencyProperty property, Type propertyType)
    {
        PropertyTypes[property] = propertyType;
        return property;
    }

    // C# 14 static extensions: add WPF 5-arg Register/RegisterAttached overloads (with ValidateValueCallback)
    // and a GlobalIndex property to Microsoft.UI.Xaml.DependencyProperty.
    extension(Microsoft.UI.Xaml.DependencyProperty)
    {
        // WPF DependencyProperty.FromName: look up a DP by name on a type.
        // Not supported by WinUI; return null (serialization-only path, gated at entry).
        public static Microsoft.UI.Xaml.DependencyProperty? FromName(string name, Type ownerType) => null;

        public static Microsoft.UI.Xaml.DependencyProperty Register(
            string name, System.Type propertyType, System.Type ownerType)
            => TrackPropertyType(
                Microsoft.UI.Xaml.DependencyProperty.Register(name, propertyType, ownerType, new Microsoft.UI.Xaml.PropertyMetadata(null)),
                propertyType);

        public static Microsoft.UI.Xaml.DependencyProperty Register(
            string name, System.Type propertyType, System.Type ownerType,
            FrameworkPropertyMetadata typeMetadata, ValidateValueCallback validateValueCallback)
            => TrackPropertyType(
                Microsoft.UI.Xaml.DependencyProperty.Register(name, propertyType, ownerType, typeMetadata),
                propertyType);

        public static Microsoft.UI.Xaml.DependencyProperty RegisterAttached(
            string name, System.Type propertyType, System.Type ownerType)
            => TrackPropertyType(
                Microsoft.UI.Xaml.DependencyProperty.RegisterAttached(name, propertyType, ownerType, new Microsoft.UI.Xaml.PropertyMetadata(null)),
                propertyType);

        public static Microsoft.UI.Xaml.DependencyProperty RegisterAttached(
            string name, System.Type propertyType, System.Type ownerType,
            FrameworkPropertyMetadata typeMetadata, ValidateValueCallback validateValueCallback)
            => TrackPropertyType(
                Microsoft.UI.Xaml.DependencyProperty.RegisterAttached(name, propertyType, ownerType, typeMetadata),
                propertyType);

        public static DependencyPropertyKey RegisterReadOnly(
            string name, System.Type propertyType, System.Type ownerType,
            Microsoft.UI.Xaml.PropertyMetadata typeMetadata)
            => new DependencyPropertyKey(TrackPropertyType(Microsoft.UI.Xaml.DependencyProperty.Register(name, propertyType, ownerType, typeMetadata), propertyType));

        public static DependencyPropertyKey RegisterReadOnly(
            string name, System.Type propertyType, System.Type ownerType,
            FrameworkPropertyMetadata typeMetadata)
            => new DependencyPropertyKey(TrackPropertyType(Microsoft.UI.Xaml.DependencyProperty.Register(name, propertyType, ownerType, typeMetadata), propertyType));

        public static DependencyPropertyKey RegisterAttachedReadOnly(
            string name, System.Type propertyType, System.Type ownerType,
            Microsoft.UI.Xaml.PropertyMetadata typeMetadata)
            => new DependencyPropertyKey(TrackPropertyType(Microsoft.UI.Xaml.DependencyProperty.RegisterAttached(name, propertyType, ownerType, typeMetadata), propertyType));

        public static DependencyPropertyKey RegisterAttachedReadOnly(
            string name, System.Type propertyType, System.Type ownerType,
            FrameworkPropertyMetadata typeMetadata)
            => new DependencyPropertyKey(TrackPropertyType(Microsoft.UI.Xaml.DependencyProperty.RegisterAttached(name, propertyType, ownerType, typeMetadata), propertyType));
    }

    // GlobalIndex: WPF assigns each DP a unique int. Shim returns a hash of the property name.
    extension(Microsoft.UI.Xaml.DependencyProperty property)
    {
        public int GlobalIndex => property.GetHashCode();

        public string Name => property.ToString();

        public Type PropertyType => PropertyTypes.TryGetValue(property, out var propertyType) ? propertyType : typeof(object);

        public bool IsValidValue(object? value) => true;

        public bool ReadOnly => false;

        // WPF DefaultMetadata: returns the PropertyMetadata registered for the property.
        // WinUI has GetMetadata(Type); fall back to object-type metadata or empty metadata.
        public Microsoft.UI.Xaml.PropertyMetadata DefaultMetadata
            => property.GetMetadata(typeof(object)) ?? new Microsoft.UI.Xaml.PropertyMetadata(null);

        // WPF OwnerType: the type that originally registered the property.
        // Not tracked by WinUI; return null as shim (serialization-only path).
        public Type? OwnerType => null;

        // WPF DependencyProperty.GetDefaultValue(Type) — returns the registered
        // default for that owner type. WinUI exposes PropertyMetadata.DefaultValue
        // via GetMetadata; if no metadata is available we fall back to the
        // type-system default.
        public object? GetDefaultValue(Type forType)
        {
            var metadata = property.GetMetadata(forType);
            return metadata?.DefaultValue;
        }
    }

    extension(object value)
    {
        public bool IsValid(DependencyProperty property) => true;
    }

    public static Microsoft.UI.Xaml.DependencyProperty AddOwner(
        this Microsoft.UI.Xaml.DependencyProperty property,
        System.Type ownerType) => property;

    public static Microsoft.UI.Xaml.DependencyProperty AddOwner(
        this Microsoft.UI.Xaml.DependencyProperty property,
        System.Type ownerType,
        Microsoft.UI.Xaml.PropertyMetadata typeMetadata) => property;

    // Accept WPF-style FrameworkPropertyMetadata too (used by linked WPF source files).
    public static Microsoft.UI.Xaml.DependencyProperty AddOwner(
        this Microsoft.UI.Xaml.DependencyProperty property,
        System.Type ownerType,
        FrameworkPropertyMetadata typeMetadata) => property;

    // WPF OverrideMetadata: registers a per-type metadata override.
    // Shim is a no-op since WinUI doesn't support per-type metadata.
    public static void OverrideMetadata(
        this Microsoft.UI.Xaml.DependencyProperty property,
        System.Type forType,
        Microsoft.UI.Xaml.PropertyMetadata typeMetadata) { }

    public static void OverrideMetadata(
        this Microsoft.UI.Xaml.DependencyProperty property,
        System.Type forType,
        FrameworkPropertyMetadata typeMetadata) { }
}
