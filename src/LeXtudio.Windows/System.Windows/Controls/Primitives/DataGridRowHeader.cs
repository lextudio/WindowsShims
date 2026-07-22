namespace System.Windows.Controls.Primitives;

// Uno-specific partial for the linked upstream DataGridRowHeader. The upstream
// file supplies the full WPF behavior (content/width/selection coercion, click
// selection, visual-state machine); this partial only adds the shim grid-line
// border the Uno render path draws in place of WPF's template chrome.
public partial class DataGridRowHeader
{
    public bool HasShimGridLine { get; private set; }

    // The upstream ParentRow walks the visual tree, which may not yet resolve to
    // the owning DataGridRow when the header is built or when grid-line/content
    // notifications fire. DataGridRow records itself here as a reliable fallback.
    private DataGridRow? _shimOwnerRow;

    internal void SetShimOwnerRow(DataGridRow row) => _shimOwnerRow = row;

    internal DataGridRow? EffectiveRow => ParentRow ?? _shimOwnerRow;

    // Session 122: unlike DataGridColumnHeader (which has its own
    // HeaderTemplateXaml/Border chrome), DataGridRowHeader previously had no
    // ControlTemplate of its own at all, so it fell back to whatever the
    // default ButtonBase/Control template on this Uno target provides — which
    // has no Border in it. Setting BorderBrush/BorderThickness below (the same
    // ApplyShimGridLines pattern every other cell/header type uses) was
    // therefore a complete visual no-op: the DPs were set correctly but nothing
    // in the visual tree consumed them, so the whole row-header column (a real,
    // correctly-sized 24px-by-default area — RowHeaderShimWidth/SyncProperties'
    // WidthProperty transfer both work) rendered as a plain, borderless blank
    // strip. A minimal template (Border + ContentPresenter, mirroring
    // DataGridColumnHeader's) fixes this the same way ApplyShimGridLines'
    // eager ApplyTemplate() call already does for column headers.
    private const string RowHeaderTemplateXaml =
        "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' " +
        "xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>" +
        "<Border Background='{TemplateBinding Background}' " +
        "BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='{TemplateBinding BorderThickness}'>" +
        "<ContentPresenter Content='{TemplateBinding Content}' " +
        "HorizontalAlignment='Center' VerticalAlignment='Center' />" +
        "</Border></ControlTemplate>";

    private static Microsoft.UI.Xaml.Controls.ControlTemplate? _rowHeaderTemplate;

    internal void ApplyShimGridLines()
    {
        HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch;
        VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Stretch;
        MinHeight = 32;
        var owner = EffectiveRow?.DataGridOwner;
        var themeElement = owner is Microsoft.UI.Xaml.FrameworkElement ownerElement
            ? ownerElement
            : this;
        Background = DataGridFluentTheme.RowHeaderBackgroundFor(themeElement);
        Foreground = EffectiveRow?.IsSelected == true
            ? DataGridFluentTheme.SelectionForegroundFor(themeElement)
            : DataGridFluentTheme.SecondaryTextFor(themeElement);
        var visibility = owner?.GridLinesVisibility ?? DataGridGridLinesVisibility.None;
        var horizontal = visibility is DataGridGridLinesVisibility.All or DataGridGridLinesVisibility.Horizontal;
        var vertical = visibility is DataGridGridLinesVisibility.All or DataGridGridLinesVisibility.Vertical;

        HasShimGridLine = horizontal || vertical;
        BorderThickness = HasShimGridLine
            ? new Microsoft.UI.Xaml.Thickness(0, 0, vertical ? 1 : 0, horizontal ? 1 : 0)
            : new Microsoft.UI.Xaml.Thickness(0);
        BorderBrush = HasShimGridLine
            ? (vertical ? owner?.VerticalGridLinesBrush : owner?.HorizontalGridLinesBrush)
            : null;

        if (_rowHeaderTemplate is null)
        {
            _rowHeaderTemplate = (Microsoft.UI.Xaml.Controls.ControlTemplate)
                Microsoft.UI.Xaml.Markup.XamlReader.Load(RowHeaderTemplateXaml);
        }

        Template = _rowHeaderTemplate;
        ApplyTemplate();
    }
}
