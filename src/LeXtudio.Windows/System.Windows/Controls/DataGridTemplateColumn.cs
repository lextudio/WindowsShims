using System.Windows.Data;
using WinUIContentPresenter = Microsoft.UI.Xaml.Controls.ContentPresenter;
using WinUIDataTemplate = Microsoft.UI.Xaml.DataTemplate;
using WinUIDataTemplateSelector = Microsoft.UI.Xaml.Controls.DataTemplateSelector;

namespace System.Windows.Controls;

public partial class DataGridTemplateColumn : DataGridColumn
{
    public static readonly DependencyProperty CellTemplateProperty =
        DependencyProperty.Register(
            nameof(CellTemplate),
            typeof(WinUIDataTemplate),
            typeof(DataGridTemplateColumn),
            new FrameworkPropertyMetadata(null, (PropertyChangedCallback)NotifyPropertyChangeForRefreshContent));

    public static readonly DependencyProperty CellTemplateSelectorProperty =
        DependencyProperty.Register(
            nameof(CellTemplateSelector),
            typeof(WinUIDataTemplateSelector),
            typeof(DataGridTemplateColumn),
            new FrameworkPropertyMetadata(null, (PropertyChangedCallback)NotifyPropertyChangeForRefreshContent));

    public static readonly DependencyProperty CellEditingTemplateProperty =
        DependencyProperty.Register(
            nameof(CellEditingTemplate),
            typeof(WinUIDataTemplate),
            typeof(DataGridTemplateColumn),
            new FrameworkPropertyMetadata(null, (PropertyChangedCallback)NotifyPropertyChangeForRefreshContent));

    public static readonly DependencyProperty CellEditingTemplateSelectorProperty =
        DependencyProperty.Register(
            nameof(CellEditingTemplateSelector),
            typeof(WinUIDataTemplateSelector),
            typeof(DataGridTemplateColumn),
            new FrameworkPropertyMetadata(null, (PropertyChangedCallback)NotifyPropertyChangeForRefreshContent));

    public WinUIDataTemplate? CellTemplate
    {
        get => (WinUIDataTemplate?)GetValue(CellTemplateProperty);
        set => SetValue(CellTemplateProperty, value);
    }

    public WinUIDataTemplateSelector? CellTemplateSelector
    {
        get => (WinUIDataTemplateSelector?)GetValue(CellTemplateSelectorProperty);
        set => SetValue(CellTemplateSelectorProperty, value);
    }

    public WinUIDataTemplate? CellEditingTemplate
    {
        get => (WinUIDataTemplate?)GetValue(CellEditingTemplateProperty);
        set => SetValue(CellEditingTemplateProperty, value);
    }

    public WinUIDataTemplateSelector? CellEditingTemplateSelector
    {
        get => (WinUIDataTemplateSelector?)GetValue(CellEditingTemplateSelectorProperty);
        set => SetValue(CellEditingTemplateSelectorProperty, value);
    }

    protected override FrameworkElement? GenerateElement(DataGridCell cell, object dataItem)
        => LoadTemplateContent(isEditing: false);

    protected override FrameworkElement? GenerateEditingElement(DataGridCell cell, object dataItem)
        => LoadTemplateContent(isEditing: true);

    protected internal override void RefreshCellContent(FrameworkElement element, string propertyName)
    {
        if (element is DataGridCell cell)
        {
            var isEditing = cell.IsEditing;
            if ((!isEditing &&
                    (string.Equals(propertyName, nameof(CellTemplate), StringComparison.Ordinal) ||
                     string.Equals(propertyName, nameof(CellTemplateSelector), StringComparison.Ordinal))) ||
                (isEditing &&
                    (string.Equals(propertyName, nameof(CellEditingTemplate), StringComparison.Ordinal) ||
                     string.Equals(propertyName, nameof(CellEditingTemplateSelector), StringComparison.Ordinal))))
            {
                cell.BuildVisualTree();
                return;
            }
        }

        base.RefreshCellContent(element, propertyName);
    }

    private FrameworkElement? LoadTemplateContent(bool isEditing)
    {
        ChooseCellTemplateAndSelector(
            isEditing,
            out var template,
            out var templateSelector);

        if (template is null && templateSelector is null)
        {
            return null;
        }

        var presenter = new WinUIContentPresenter
        {
            ContentTemplate = template,
            ContentTemplateSelector = templateSelector,
        };

        BindingOperations.SetBinding(presenter, WinUIContentPresenter.ContentProperty, new Binding());
        return presenter;
    }

    private void ChooseCellTemplateAndSelector(
        bool isEditing,
        out WinUIDataTemplate? template,
        out WinUIDataTemplateSelector? templateSelector)
    {
        template = null;
        templateSelector = null;

        if (isEditing)
        {
            template = CellEditingTemplate;
            templateSelector = CellEditingTemplateSelector;
        }

        if (template is null && templateSelector is null)
        {
            template = CellTemplate;
            templateSelector = CellTemplateSelector;
        }
    }
}
