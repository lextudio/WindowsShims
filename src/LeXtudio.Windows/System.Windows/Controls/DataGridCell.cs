namespace System.Windows.Controls;

public partial class DataGridCell : ContentControl
{
    public bool IsEditing { get; set; }

    public DataGridColumn? Column { get; set; }

    internal DataGridRow? RowOwner { get; set; }

    internal DataGrid? DataGridOwner => RowOwner?.DataGridOwner ?? Column?.DataGridOwner;

    internal object? RowDataItem => RowOwner?.Item;

    internal void BuildVisualTree()
    {
    }
}
