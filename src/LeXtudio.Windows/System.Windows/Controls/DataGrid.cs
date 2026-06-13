using System.Windows.Controls.Primitives;

namespace System.Windows.Controls;

// Uno-specific additions to the WPF DataGrid control root. Only members that
// do NOT appear in the linked upstream DataGrid.cs should live here. The
// upstream file is compiled as a partial on HAS_UNO so both parts merge.
public partial class DataGrid
{
    // UpdateVisualState: the upstream calls this (0-arg) which calls the
    // virtual ChangeVisualState. Provide the 0-arg overload in the shim part.
    internal void UpdateVisualState() => ChangeVisualState(true);

    // The WPF static-ctor OverrideMetadata(typeof(DataGrid)) call is a no-op
    // under the shim, and the library's Themes/Generic.xaml is not reliably in
    // the consumer's ms-appx resource map. So the shim assigns a minimal
    // ControlTemplate directly (built via XamlReader) — self-contained, no
    // dependency on default-style probing. The template root hosts
    // PART_ShimRowsHost, which the code render path populates.
    private const string ShimTemplateXaml =
        "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' " +
        "xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>" +
        "<Border Background='White' BorderBrush='#CCCCCC' BorderThickness='1'>" +
        "<ScrollViewer HorizontalScrollBarVisibility='Auto' VerticalScrollBarVisibility='Auto'>" +
        "<StackPanel x:Name='PART_ShimRowsHost' MinWidth='120' MinHeight='40' />" +
        "</ScrollViewer></Border></ControlTemplate>";

    private void EnsureShimStyleKey()
    {
        if (Template is not null)
        {
            return;
        }

        try
        {
            Template = (Microsoft.UI.Xaml.Controls.ControlTemplate)
                Microsoft.UI.Xaml.Markup.XamlReader.Load(ShimTemplateXaml);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DataGrid] shim template load failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    // Session 25 — shim render path. Populates PART_ShimRowsHost with a header
    // row plus one cell-panel per item, where each cell's content is produced
    // by the column's real element-generation logic and bound to the item.
    // This is intentionally simple (no virtualization, no DataGridRow visual
    // template); it is the first-visible-artifact rung, separate from the
    // upstream PART_RowsPresenter / ItemsHost machinery.
    internal void BuildShimVisualTree()
    {
        if (GetTemplateChild("PART_ShimRowsHost") is not Microsoft.UI.Xaml.Controls.Panel host)
        {
            return;
        }

        host.Children.Clear();
        host.Children.Add(BuildHeaderRow());

        foreach (var item in Items)
        {
            if (item is null)
            {
                continue;
            }

            host.Children.Add(BuildItemRow(item));
        }
    }

    private Microsoft.UI.Xaml.Controls.StackPanel BuildHeaderRow()
    {
        var header = new Microsoft.UI.Xaml.Controls.StackPanel
        {
            Orientation = Microsoft.UI.Xaml.Controls.Orientation.Horizontal,
        };

        foreach (var column in Columns)
        {
            if (!column.IsVisible)
            {
                continue;
            }

            header.Children.Add(new TextBlock
            {
                Text = column.Header?.ToString() ?? string.Empty,
                Width = ShimColumnWidth(column),
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Margin = new Microsoft.UI.Xaml.Thickness(4, 2, 4, 2),
            });
        }

        return header;
    }

    private Microsoft.UI.Xaml.Controls.StackPanel BuildItemRow(object item)
    {
        // The DataGridRow is the logical container the WPF code expects; the
        // visible row is a horizontal panel of cells whose owner is that row.
        var row = new DataGridRow();
        row.PrepareRow(item, this);

        var rowPanel = new Microsoft.UI.Xaml.Controls.StackPanel
        {
            Orientation = Microsoft.UI.Xaml.Controls.Orientation.Horizontal,
        };

        foreach (var column in Columns)
        {
            if (!column.IsVisible)
            {
                continue;
            }

            var cell = new DataGridCell
            {
                Column = column,
                RowOwner = row,
                Width = ShimColumnWidth(column),
                Margin = new Microsoft.UI.Xaml.Thickness(4, 2, 4, 2),
            };
            cell.BuildVisualTree();
            rowPanel.Children.Add(cell);
        }

        return rowPanel;
    }

    private static double ShimColumnWidth(DataGridColumn column)
        => column.ActualWidth > 0 ? column.ActualWidth : 120;
}
