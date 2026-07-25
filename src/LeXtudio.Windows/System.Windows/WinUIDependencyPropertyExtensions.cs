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
    private static readonly ConcurrentDictionary<Microsoft.UI.Xaml.DependencyProperty, Type> OwnerTypes = new();
    private static readonly ConcurrentDictionary<Microsoft.UI.Xaml.DependencyProperty, string> PropertyNames = new();

    private static Microsoft.UI.Xaml.DependencyProperty TrackPropertyType(Microsoft.UI.Xaml.DependencyProperty property, Type propertyType)
    {
        PropertyTypes[property] = propertyType;
        return property;
    }

    private static Microsoft.UI.Xaml.DependencyProperty TrackOwner(Microsoft.UI.Xaml.DependencyProperty property, Type ownerType)
    {
        OwnerTypes[property] = ownerType;
        return property;
    }

    private static Microsoft.UI.Xaml.DependencyProperty TrackName(Microsoft.UI.Xaml.DependencyProperty property, string name)
    {
        PropertyNames[property] = name;
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
            => TrackName(TrackOwner(TrackPropertyType(
                Microsoft.UI.Xaml.DependencyProperty.Register(name, propertyType, ownerType, new Microsoft.UI.Xaml.PropertyMetadata(null)),
                propertyType), ownerType), name);

        public static Microsoft.UI.Xaml.DependencyProperty Register(
            string name, System.Type propertyType, System.Type ownerType,
            FrameworkPropertyMetadata typeMetadata, ValidateValueCallback validateValueCallback)
            => TrackName(TrackOwner(TrackPropertyType(
                Microsoft.UI.Xaml.DependencyProperty.Register(name, propertyType, ownerType, typeMetadata),
                propertyType), ownerType), name);

        public static Microsoft.UI.Xaml.DependencyProperty RegisterAttached(
            string name, System.Type propertyType, System.Type ownerType)
            => TrackName(TrackOwner(TrackPropertyType(
                Microsoft.UI.Xaml.DependencyProperty.RegisterAttached(name, propertyType, ownerType, new Microsoft.UI.Xaml.PropertyMetadata(null)),
                propertyType), ownerType), name);

        public static Microsoft.UI.Xaml.DependencyProperty RegisterAttached(
            string name, System.Type propertyType, System.Type ownerType,
            FrameworkPropertyMetadata typeMetadata, ValidateValueCallback validateValueCallback)
            => TrackName(TrackOwner(TrackPropertyType(
                Microsoft.UI.Xaml.DependencyProperty.RegisterAttached(name, propertyType, ownerType, typeMetadata),
                propertyType), ownerType), name);

        public static DependencyPropertyKey RegisterReadOnly(
            string name, System.Type propertyType, System.Type ownerType,
            Microsoft.UI.Xaml.PropertyMetadata typeMetadata)
            => new DependencyPropertyKey(TrackOwner(TrackPropertyType(Microsoft.UI.Xaml.DependencyProperty.Register(name, propertyType, ownerType, typeMetadata), propertyType), ownerType));

        public static DependencyPropertyKey RegisterReadOnly(
            string name, System.Type propertyType, System.Type ownerType,
            FrameworkPropertyMetadata typeMetadata)
            => new DependencyPropertyKey(TrackOwner(TrackPropertyType(Microsoft.UI.Xaml.DependencyProperty.Register(name, propertyType, ownerType, typeMetadata), propertyType), ownerType));

        public static DependencyPropertyKey RegisterAttachedReadOnly(
            string name, System.Type propertyType, System.Type ownerType,
            Microsoft.UI.Xaml.PropertyMetadata typeMetadata)
            => new DependencyPropertyKey(TrackOwner(TrackPropertyType(Microsoft.UI.Xaml.DependencyProperty.RegisterAttached(name, propertyType, ownerType, typeMetadata), propertyType), ownerType));

        public static DependencyPropertyKey RegisterAttachedReadOnly(
            string name, System.Type propertyType, System.Type ownerType,
            FrameworkPropertyMetadata typeMetadata)
            => new DependencyPropertyKey(TrackOwner(TrackPropertyType(Microsoft.UI.Xaml.DependencyProperty.RegisterAttached(name, propertyType, ownerType, typeMetadata), propertyType), ownerType));
    }

    private static readonly System.Reflection.FieldInfo? _dpNameField =
        typeof(Microsoft.UI.Xaml.DependencyProperty).GetField("_name",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

    private static string GetDependencyPropertyName(Microsoft.UI.Xaml.DependencyProperty dp)
    {
        if (PropertyNames.TryGetValue(dp, out var tracked)) return tracked;
        if (_dpNameField != null && _dpNameField.GetValue(dp) is string name) return name;
        return dp.ToString();
    }

    // GlobalIndex: WPF assigns each DP a unique int. Shim returns a hash of the property name.
    extension(Microsoft.UI.Xaml.DependencyProperty property)
    {
        public int GlobalIndex => property.GetHashCode();

        public string Name => GetDependencyPropertyName(property);

        public Type PropertyType => PropertyTypes.TryGetValue(property, out var propertyType) ? propertyType : typeof(object);

        public bool IsValidValue(object? value) => true;

        public bool ReadOnly => false;

        // WPF DefaultMetadata: returns the PropertyMetadata registered for the property.
        // WinUI has GetMetadata(Type); fall back to object-type metadata or empty metadata.
        public Microsoft.UI.Xaml.PropertyMetadata DefaultMetadata
            => property.GetMetadata(typeof(object)) ?? new Microsoft.UI.Xaml.PropertyMetadata(null);

        // WPF OwnerType: the type that originally registered the property.
        // Tracked via OwnerTypes dictionary populated by Register/RegisterAttached shims.
        public Type? OwnerType => OwnerTypes.TryGetValue(property, out var ownerType) ? ownerType : null;

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
