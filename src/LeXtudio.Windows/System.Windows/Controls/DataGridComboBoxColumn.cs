using System.Collections;
using System.Windows.Data;
using WinUIComboBox = Microsoft.UI.Xaml.Controls.ComboBox;

namespace System.Windows.Controls;

public partial class DataGridComboBoxColumn : DataGridColumn
{
    private static Style? _defaultElementStyle;

    private BindingBase? _selectedValueBinding;
    private BindingBase? _selectedItemBinding;
    private BindingBase? _textBinding;

    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(
            nameof(ItemsSource),
            typeof(IEnumerable),
            typeof(DataGridComboBoxColumn),
            new FrameworkPropertyMetadata(null, (PropertyChangedCallback)NotifyPropertyChangeForRefreshContent));

    public static readonly DependencyProperty DisplayMemberPathProperty =
        DependencyProperty.Register(
            nameof(DisplayMemberPath),
            typeof(string),
            typeof(DataGridComboBoxColumn),
            new FrameworkPropertyMetadata(string.Empty, (PropertyChangedCallback)NotifyPropertyChangeForRefreshContent));

    public static readonly DependencyProperty SelectedValuePathProperty =
        DependencyProperty.Register(
            nameof(SelectedValuePath),
            typeof(string),
            typeof(DataGridComboBoxColumn),
            new FrameworkPropertyMetadata(string.Empty, (PropertyChangedCallback)NotifyPropertyChangeForRefreshContent));

    public static readonly DependencyProperty ElementStyleProperty =
        DependencyProperty.Register(
            nameof(ElementStyle),
            typeof(Style),
            typeof(DataGridComboBoxColumn),
            new FrameworkPropertyMetadata(null, (PropertyChangedCallback)NotifyPropertyChangeForRefreshContent));

    public static readonly DependencyProperty EditingElementStyleProperty =
        DependencyProperty.Register(
            nameof(EditingElementStyle),
            typeof(Style),
            typeof(DataGridComboBoxColumn),
            new FrameworkPropertyMetadata(null, (PropertyChangedCallback)NotifyPropertyChangeForRefreshContent));

    public static Style DefaultElementStyle
        => _defaultElementStyle ??= new Style(typeof(WinUIComboBox));

    public static Style DefaultEditingElementStyle => DefaultElementStyle;

    public virtual BindingBase? SelectedValueBinding
    {
        get => _selectedValueBinding;
        set
        {
            if (!ReferenceEquals(_selectedValueBinding, value))
            {
                var oldBinding = _selectedValueBinding;
                _selectedValueBinding = value;
                OnSelectedValueBindingChanged(oldBinding, _selectedValueBinding);
            }
        }
    }

    public virtual BindingBase? SelectedItemBinding
    {
        get => _selectedItemBinding;
        set
        {
            if (!ReferenceEquals(_selectedItemBinding, value))
            {
                var oldBinding = _selectedItemBinding;
                _selectedItemBinding = value;
                OnSelectedItemBindingChanged(oldBinding, _selectedItemBinding);
            }
        }
    }

    public virtual BindingBase? TextBinding
    {
        get => _textBinding;
        set
        {
            if (!ReferenceEquals(_textBinding, value))
            {
                var oldBinding = _textBinding;
                _textBinding = value;
                OnTextBindingChanged(oldBinding, _textBinding);
            }
        }
    }

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public string DisplayMemberPath
    {
        get => (string)GetValue(DisplayMemberPathProperty);
        set => SetValue(DisplayMemberPathProperty, value);
    }

    public string SelectedValuePath
    {
        get => (string)GetValue(SelectedValuePathProperty);
        set => SetValue(SelectedValuePathProperty, value);
    }

    public Style? ElementStyle
    {
        get => (Style?)GetValue(ElementStyleProperty);
        set => SetValue(ElementStyleProperty, value);
    }

    public Style? EditingElementStyle
    {
        get => (Style?)GetValue(EditingElementStyleProperty);
        set => SetValue(EditingElementStyleProperty, value);
    }

    public override BindingBase? ClipboardContentBinding
    {
        get => base.ClipboardContentBinding ?? EffectiveBinding;
        set => base.ClipboardContentBinding = value;
    }

    private BindingBase? EffectiveBinding
        => SelectedItemBinding ?? SelectedValueBinding ?? TextBinding;

    protected virtual void OnSelectedValueBindingChanged(BindingBase? oldBinding, BindingBase? newBinding)
        => NotifyPropertyChanged(nameof(SelectedValueBinding));

    protected virtual void OnSelectedItemBindingChanged(BindingBase? oldBinding, BindingBase? newBinding)
        => NotifyPropertyChanged(nameof(SelectedItemBinding));

    protected virtual void OnTextBindingChanged(BindingBase? oldBinding, BindingBase? newBinding)
        => NotifyPropertyChanged(nameof(TextBinding));

    protected override bool OnCoerceIsReadOnly(bool baseValue)
        => DataGridHelper.IsOneWay(EffectiveBinding) || base.OnCoerceIsReadOnly(baseValue);

    protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
        => GenerateComboBox(isEditing: false);

    protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
        => GenerateComboBox(isEditing: true);

    protected internal override void RefreshCellContent(FrameworkElement element, string propertyName)
    {
        if (element is DataGridCell cell)
        {
            var isCellEditing = cell.IsEditing;
            if ((string.Equals(propertyName, nameof(ElementStyle), StringComparison.Ordinal) && !isCellEditing) ||
                (string.Equals(propertyName, nameof(EditingElementStyle), StringComparison.Ordinal) && isCellEditing))
            {
                cell.BuildVisualTree();
                return;
            }

            if (cell.Content is WinUIComboBox comboBox)
            {
                switch (propertyName)
                {
                    case nameof(SelectedItemBinding):
                        ApplyBinding(SelectedItemBinding, comboBox, WinUIComboBox.SelectedItemProperty);
                        return;
                    case nameof(SelectedValueBinding):
                        ApplyBinding(SelectedValueBinding, comboBox, WinUIComboBox.SelectedValueProperty);
                        return;
                    case nameof(TextBinding):
                        ApplyBinding(TextBinding, comboBox, WinUIComboBox.TextProperty);
                        return;
                    case nameof(SelectedValuePath):
                        comboBox.SelectedValuePath = SelectedValuePath;
                        return;
                    case nameof(DisplayMemberPath):
                        comboBox.DisplayMemberPath = DisplayMemberPath;
                        return;
                    case nameof(ItemsSource):
                        comboBox.ItemsSource = ItemsSource;
                        return;
                }
            }
        }

        base.RefreshCellContent(element, propertyName);
    }

    protected override object? PrepareCellForEdit(FrameworkElement editingElement, RoutedEventArgs editingEventArgs)
    {
        if (editingElement is WinUIComboBox comboBox)
        {
            comboBox.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
            return GetComboBoxSelectionValue(comboBox);
        }

        return null;
    }

    private WinUIComboBox GenerateComboBox(bool isEditing)
    {
        var comboBox = new WinUIComboBox();

        ApplyStyle(isEditing, comboBox);
        ApplyColumnProperties(comboBox);

        return comboBox;
    }

    private void ApplyStyle(bool isEditing, FrameworkElement element)
    {
        var style = (isEditing ? EditingElementStyle : ElementStyle) ?? (isEditing ? ElementStyle : null);
        if (style is not null)
        {
            element.Style = style;
        }
    }

    private void ApplyColumnProperties(WinUIComboBox comboBox)
    {
        ApplyBinding(SelectedItemBinding, comboBox, WinUIComboBox.SelectedItemProperty);
        ApplyBinding(SelectedValueBinding, comboBox, WinUIComboBox.SelectedValueProperty);
        ApplyBinding(TextBinding, comboBox, WinUIComboBox.TextProperty);

        comboBox.SelectedValuePath = SelectedValuePath;
        comboBox.DisplayMemberPath = DisplayMemberPath;
        comboBox.ItemsSource = ItemsSource;
    }

    private object? GetComboBoxSelectionValue(WinUIComboBox comboBox)
    {
        if (SelectedItemBinding is not null)
        {
            return comboBox.SelectedItem;
        }

        if (SelectedValueBinding is not null)
        {
            return comboBox.SelectedValue;
        }

        return comboBox.Text;
    }

    private static void ApplyBinding(BindingBase? binding, DependencyObject target, DependencyProperty property)
    {
        if (binding is not null)
        {
            BindingOperations.SetBinding(target, property, binding);
        }
        else
        {
            BindingOperations.ClearBinding(target, property);
        }
    }
}
