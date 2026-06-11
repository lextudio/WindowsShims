using WinUICheckBox = Microsoft.UI.Xaml.Controls.CheckBox;

namespace System.Windows.Controls;

public partial class DataGridCheckBoxColumn : DataGridBoundColumn
{
    private static Style? _defaultElementStyle;
    private static Style? _defaultEditingElementStyle;

    public static readonly DependencyProperty IsThreeStateProperty =
        DependencyProperty.Register(
            nameof(IsThreeState),
            typeof(bool),
            typeof(DataGridCheckBoxColumn),
            new FrameworkPropertyMetadata(false, (PropertyChangedCallback)NotifyPropertyChangeForRefreshContent));

    public static Style DefaultElementStyle
        => _defaultElementStyle ??= new Style(typeof(WinUICheckBox));

    public static Style DefaultEditingElementStyle
        => _defaultEditingElementStyle ??= new Style(typeof(WinUICheckBox));

    public bool IsThreeState
    {
        get => (bool)GetValue(IsThreeStateProperty);
        set => SetValue(IsThreeStateProperty, value);
    }

    protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
        => GenerateCheckBox(isEditing: false, cell);

    protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
        => GenerateCheckBox(isEditing: true, cell);

    protected internal override void RefreshCellContent(FrameworkElement element, string propertyName)
    {
        if (element is DataGridCell { Content: WinUICheckBox checkBox } &&
            string.Equals(propertyName, nameof(IsThreeState), StringComparison.Ordinal))
        {
            checkBox.IsThreeState = IsThreeState;
            return;
        }

        base.RefreshCellContent(element, propertyName);
    }

    protected override object? PrepareCellForEdit(FrameworkElement editingElement, RoutedEventArgs editingEventArgs)
    {
        if (editingElement is WinUICheckBox checkBox)
        {
            checkBox.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
            return checkBox.IsChecked;
        }

        return false;
    }

    private WinUICheckBox GenerateCheckBox(bool isEditing, DataGridCell cell)
    {
        var checkBox = cell.Content as WinUICheckBox ?? new WinUICheckBox();

        checkBox.IsThreeState = IsThreeState;
        ApplyStyle(isEditing, defaultToElementStyle: true, checkBox);
        ApplyBinding(checkBox, WinUICheckBox.IsCheckedProperty);

        return checkBox;
    }
}
