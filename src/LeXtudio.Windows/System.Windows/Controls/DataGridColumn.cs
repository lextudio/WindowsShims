namespace System.Windows.Controls;

// Session 63: the WPF DataGridColumn body is linked upstream. This partial
// carries only Uno render/edit bridge helpers used by the local visual path.
public abstract partial class DataGridColumn
{
    public void SetValue(DependencyPropertyKey key, object? value)
        => SetValue(key.DependencyProperty, value);

    public void CoerceValue(DependencyProperty dp)
    {
        // The Uno DP shim does not run WPF coercion callbacks. Width and
        // transfer-property behavior is still handled by the local DataGrid
        // render pass, so this bridge is intentionally a compile/runtime no-op.
    }

    internal FrameworkElement? BuildCellContent(DataGridCell cell, object dataItem)
        => GenerateElement(cell, dataItem);

    internal FrameworkElement? BuildEditingCellContent(DataGridCell cell, object dataItem)
        => GenerateEditingElement(cell, dataItem);

}
