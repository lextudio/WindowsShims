namespace System.Windows;

/// <summary>
/// WPF compatibility extension methods on Microsoft.UI.Xaml.DependencyProperty.
/// AddOwner is a WPF API for sharing a DependencyProperty across multiple owner types;
/// WinUI/Uno doesn't natively support this so we return the same instance.
/// Used by WPF source files like TableColumn that call Panel.BackgroundProperty.AddOwner(...).
/// </summary>
public static class WinUIDependencyPropertyExtensions
{
    // C# 14 static extensions: add WPF 5-arg Register/RegisterAttached overloads (with ValidateValueCallback)
    // and a GlobalIndex property to Microsoft.UI.Xaml.DependencyProperty.
    extension(Microsoft.UI.Xaml.DependencyProperty)
    {
        public static Microsoft.UI.Xaml.DependencyProperty Register(
            string name, System.Type propertyType, System.Type ownerType,
            FrameworkPropertyMetadata typeMetadata, ValidateValueCallback validateValueCallback)
            => Microsoft.UI.Xaml.DependencyProperty.Register(name, propertyType, ownerType, typeMetadata);

        public static Microsoft.UI.Xaml.DependencyProperty RegisterAttached(
            string name, System.Type propertyType, System.Type ownerType,
            FrameworkPropertyMetadata typeMetadata, ValidateValueCallback validateValueCallback)
            => Microsoft.UI.Xaml.DependencyProperty.RegisterAttached(name, propertyType, ownerType, typeMetadata);
    }

    // GlobalIndex: WPF assigns each DP a unique int. Shim returns a hash of the property name.
    extension(Microsoft.UI.Xaml.DependencyProperty property)
    {
        public int GlobalIndex => property.GetHashCode();

        public string Name => property.ToString();

        public Type PropertyType => typeof(object);

        public bool IsValidValue(object? value) => true;
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
