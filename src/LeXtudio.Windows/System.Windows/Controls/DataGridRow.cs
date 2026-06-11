namespace System.Windows.Controls;

public partial class DataGridRow : Control
{
    public object? Item { get; set; }

    public bool IsEditing { get; internal set; }

    internal DataGrid? DataGridOwner { get; set; }
}
