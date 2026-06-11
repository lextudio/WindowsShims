using WinUITextBox = Microsoft.UI.Xaml.Controls.TextBox;

namespace System.Windows.Controls;

public partial class DataGridTextColumn : DataGridBoundColumn
{
    private static Style? _defaultElementStyle;
    private static Style? _defaultEditingElementStyle;

    public static Style DefaultElementStyle
        => _defaultElementStyle ??= new Style(typeof(TextBlock));

    public static Style DefaultEditingElementStyle
        => _defaultEditingElementStyle ??= new Style(typeof(WinUITextBox));

    protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
    {
        var textBlock = new TextBlock();

        ApplyStyle(isEditing: false, defaultToElementStyle: false, textBlock);
        ApplyBinding(textBlock, TextBlock.TextProperty);

        return textBlock;
    }

    protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
    {
        var textBox = new WinUITextBox();

        ApplyStyle(isEditing: true, defaultToElementStyle: false, textBox);
        ApplyBinding(textBox, WinUITextBox.TextProperty);

        return textBox;
    }

    protected override object? PrepareCellForEdit(FrameworkElement editingElement, RoutedEventArgs editingEventArgs)
    {
        if (editingElement is WinUITextBox textBox)
        {
            textBox.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
            textBox.SelectAll();
            return textBox.Text;
        }

        return null;
    }
}
