namespace System.Windows.Controls.Primitives;

// Uno-specific partial for the linked upstream DataGridRowHeader. The upstream
// file supplies the full WPF behavior (content/width/selection coercion, click
// selection, visual-state machine); this partial only adds the shim grid-line
// border the Uno render path draws in place of WPF's template chrome.
public partial class DataGridRowHeader
{
    public bool HasShimGridLine { get; private set; }

    // Session 130 slice 10 (item 5): narrow CoerceValue activation, same
    // pattern as DataGridCell — hide the base no-op for this one control, run
    // ONLY the whitelisted CoerceValueCallback:
    //   IsRowSelected — OnCoerceIsRowSelected mirrors the owning row's
    //     IsSelected (upstream DataGridRowHeader.cs:537; triggers: SyncProperties
    //     :239 when the parent row is attached, and NotifyPropertyChanged :284
    //     when the row's IsSelected changes). Reading the row's selection is a
    //     pure mirror — no interaction with the shim's selection/visual-state
    //     logic, which already keys off the row itself.
    // The callback is invoked directly rather than via GetMetadata:
    // OverrideMetadata is a project-wide no-op (see session 130 findings).
    internal new void CoerceValue(DependencyProperty property)
    {
        if (property == IsRowSelectedProperty)
        {
            var current = GetValue(property);
            // Upstream OnCoerceIsRowSelected walks the TemplatedParent chain
            // (DataGridHelper.FindParent), which the shim's manually-placed
            // header doesn't reliably provide; EffectiveRow is the shim's
            // ParentRow equivalent (visual parent with explicit owner fallback).
            var coerced = EffectiveRow is { } row ? row.IsSelected : current;
            if (!Equals(coerced, current))
            {
                SetValue(IsRowSelectedPropertyKey.DependencyProperty, coerced);
            }
        }
    }

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
