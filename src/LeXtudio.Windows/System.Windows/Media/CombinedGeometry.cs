namespace System.Windows.Media;

// WPF CombinedGeometry shim for Uno Platform.
//
// Microsoft.UI.Xaml.Media.Geometry has only an internal constructor and cannot be
// subclassed from external assemblies. CombinedGeometry is therefore a standalone
// class with an implicit conversion operator to Geometry (= Microsoft.UI.Xaml.Media.Geometry).
//
// In practice the conversion is never reached: GetFrozenClipForCell always returns null
// so the CombinedGeometry constructor in OnCoerceClip is dead code on the HAS_UNO path.
// Also, ClipProperty.OverrideMetadata is inside the #if !HAS_UNO static cctor so
// OnCoerceClip is not registered as a callback at all.
//
// TODO: when frozen-column clipping is implemented, replace this with a Win2D/Uno2D
// bridge: CanvasGeometry.CombineWith(mode) → PathGeometry → UIElement.Clip.
public sealed class CombinedGeometry
{
    public CombinedGeometry() { }

    public CombinedGeometry(Geometry? geometry1, Geometry? geometry2)
    {
        Geometry1 = geometry1;
        Geometry2 = geometry2;
    }

    public CombinedGeometry(GeometryCombineMode geometryCombineMode, Geometry? geometry1, Geometry? geometry2)
    {
        GeometryCombineMode = geometryCombineMode;
        Geometry1 = geometry1;
        Geometry2 = geometry2;
    }

    public Geometry? Geometry1 { get; set; }
    public Geometry? Geometry2 { get; set; }
    public GeometryCombineMode GeometryCombineMode { get; set; }
    public Microsoft.UI.Xaml.Media.Transform? Transform { get; set; }

    public static implicit operator Geometry(CombinedGeometry _)
        => new Microsoft.UI.Xaml.Media.RectangleGeometry();
}
