namespace System.Windows.Controls;

public class DataGridColumnEventArgs : EventArgs
{
    public DataGridColumnEventArgs(DataGridColumn column)
    {
        Column = column;
    }

    public DataGridColumn Column { get; }
}

public class DataGridSortingEventArgs : DataGridColumnEventArgs
{
    public DataGridSortingEventArgs(DataGridColumn column)
        : base(column)
    {
    }

    public bool Handled { get; set; }
}

public class DataGridColumnReorderingEventArgs : DataGridColumnEventArgs
{
    public DataGridColumnReorderingEventArgs(DataGridColumn dataGridColumn)
        : base(dataGridColumn)
    {
    }

    public bool Cancel { get; set; }

    public Control? DropLocationIndicator { get; set; }

    public Control? DragIndicator { get; set; }
}

public class DataGridAutoGeneratingColumnEventArgs : EventArgs
{
    public DataGridAutoGeneratingColumnEventArgs(
        string propertyName,
        Type propertyType,
        DataGridColumn column)
        : this(column, propertyName, propertyType, null)
    {
    }

    internal DataGridAutoGeneratingColumnEventArgs(
        DataGridColumn column,
        string propertyName,
        Type propertyType,
        object? propertyDescriptor)
    {
        Column = column;
        PropertyName = propertyName;
        PropertyType = propertyType;
        PropertyDescriptor = propertyDescriptor;
    }

    public DataGridColumn Column { get; set; }

    public string PropertyName { get; }

    public Type PropertyType { get; }

    public object? PropertyDescriptor { get; }

    public bool Cancel { get; set; }
}

public class DataGridCellClipboardEventArgs : EventArgs
{
    public DataGridCellClipboardEventArgs(object item, DataGridColumn column, object? content)
    {
        Item = item;
        Column = column;
        Content = content;
    }

    public object Item { get; }

    public DataGridColumn Column { get; }

    public object? Content { get; set; }
}
