namespace System.Windows.Controls;

public partial class DataGridCell : ContentControl
{
    public bool IsEditing { get; set; }

    public DataGridColumn? Column { get; set; }

    internal void BuildVisualTree()
    {
    }
}
