using System.Windows.Data;

namespace System.Windows.Controls;

public static class WpfTemplateFactory
{
    public static T Create<T>(
        object? dataContext,
        Action<T>? initialize = null,
        params BindingAssignment[] bindings)
        where T : Microsoft.UI.Xaml.FrameworkElement, new()
    {
        var element = new T();
        initialize?.Invoke(element);
        foreach (var binding in bindings)
        {
            binding.Apply(element, dataContext);
        }

        return element;
    }

    public static void ApplyColumns(DataGrid grid, params DataGridColumnSpec[] columns)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(columns);

        foreach (var column in columns)
        {
            grid.Columns.Add(column.CreateColumn());
        }
    }
}

public sealed class BindingAssignment
{
    public BindingAssignment(string propertyName, Binding binding)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        ArgumentNullException.ThrowIfNull(binding);
        PropertyName = propertyName;
        Binding = binding;
    }

    public string PropertyName { get; }

    public Binding Binding { get; }

    public void Apply(object target, object? dataContext)
        => BindingEvaluator.Apply(target, PropertyName, dataContext, Binding);

    public static BindingAssignment To(string propertyName, Binding binding)
        => new(propertyName, binding);
}

public sealed class DataGridColumnSpec
{
    private DataGridColumnSpec(DataGridColumnKind kind, object? header, Binding binding, bool isReadOnly)
    {
        Kind = kind;
        Header = header;
        Binding = binding;
        IsReadOnly = isReadOnly;
    }

    public DataGridColumnKind Kind { get; }

    public object? Header { get; }

    public Binding Binding { get; }

    public bool IsReadOnly { get; }

    public DataGridColumn CreateColumn()
    {
        DataGridBoundColumn column = Kind switch
        {
            DataGridColumnKind.CheckBox => new DataGridCheckBoxColumn(),
            _ => new DataGridTextColumn(),
        };

        column.Header = Header;
        column.Binding = Binding;
        column.IsReadOnly = IsReadOnly;
        return column;
    }

    public static DataGridColumnSpec Text(object? header, Binding binding, bool isReadOnly = true)
        => new(DataGridColumnKind.Text, header, binding, isReadOnly);

    public static DataGridColumnSpec CheckBox(object? header, Binding binding, bool isReadOnly = true)
        => new(DataGridColumnKind.CheckBox, header, binding, isReadOnly);
}

public enum DataGridColumnKind
{
    Text,
    CheckBox,
}
