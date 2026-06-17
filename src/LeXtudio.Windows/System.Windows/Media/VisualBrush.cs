namespace System.Windows.Media;

public enum BrushMappingMode
{
    Absolute,
    RelativeToBoundingBox,
}

// On WinUI, SolidColorBrush is sealed, so the shim derives from the subclassable
// XamlCompositionBrushBase instead. On the Uno desktop target SolidColorBrush is not sealed, so we
// keep deriving from it (it also gives the transparent Color default used as a placeholder fill).
public class VisualBrush :
#if WINDOWS_APP_SDK
    Microsoft.UI.Xaml.Media.XamlCompositionBrushBase
#else
    Microsoft.UI.Xaml.Media.SolidColorBrush
#endif
{
    public VisualBrush()
    {
#if !WINDOWS_APP_SDK
        Color = global::Windows.UI.Color.FromArgb(0, 0, 0, 0);
#endif
    }

    public VisualBrush(UIElement visual)
        : this()
    {
        Visual = visual;
    }

    public UIElement? Visual { get; set; }

    public Rect Viewbox { get; set; }

    public BrushMappingMode ViewboxUnits { get; set; }
}
