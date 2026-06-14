namespace System.Windows.Media;

public enum BrushMappingMode
{
    Absolute,
    RelativeToBoundingBox,
}

public class VisualBrush : Microsoft.UI.Xaml.Media.SolidColorBrush
{
    public VisualBrush()
    {
        Color = global::Windows.UI.Color.FromArgb(0, 0, 0, 0);
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
