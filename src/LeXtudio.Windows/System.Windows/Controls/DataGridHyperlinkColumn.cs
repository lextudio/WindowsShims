namespace System.Windows.Controls;

// Minimal placeholder so linked DataGridColumn.CreateDefaultColumn can compile.
// Hyperlink rendering remains deferred until navigation/routed-command support.
public class DataGridHyperlinkColumn : DataGridBoundColumn
{
    protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
        => new TextBlock();

    protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
        => GenerateElement(cell, dataItem);
}
