using System.Collections;
using System.Reflection;
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
        => GenerateComboBox(isEditing: false, cell);

    protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
        => GenerateComboBox(isEditing: true, cell);

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

    private WinUIComboBox GenerateComboBox(bool isEditing, DataGridCell cell)
    {
        var comboBox = new WinUIComboBox();

        ApplyStyle(isEditing, comboBox);
        ApplyColumnProperties(comboBox);

        // Selection edits in place: write the chosen value back to the item
        // through the effective binding's path (unless read-only).
        var (path, kind) = EffectiveWriteTarget();
        var item = cell.RowDataItem;
        var prop = item is not null && path is { Length: > 0 } ? item.GetType().GetProperty(path) : null;
        var readOnly = cell.DataGridOwner?.IsCellEffectivelyReadOnly(this) ?? false;

        comboBox.IsEnabled = !readOnly;
        if (item is not null && prop is { CanWrite: true } && !readOnly)
        {
            comboBox.SelectionChanged += (_, _) =>
            {
                object? value = kind switch
                {
                    1 => comboBox.SelectedItem,
                    2 => comboBox.Text,
                    _ => comboBox.SelectedValue,
                };
                WriteBack(prop, item, value);
            };
        }

        return comboBox;
    }

    // WPF EffectiveBinding priority: SelectedItem ?? SelectedValue ?? Text.
    // kind: 1 = SelectedItem, 0 = SelectedValue, 2 = Text.
    private (string? path, int kind) EffectiveWriteTarget()
    {
        if (SelectedItemBinding is not null) return (GetBindingPath(SelectedItemBinding), 1);
        if (SelectedValueBinding is not null) return (GetBindingPath(SelectedValueBinding), 0);
        if (TextBinding is not null) return (GetBindingPath(TextBinding), 2);
        return (null, -1);
    }

    private static string? GetBindingPath(BindingBase? binding)
        => binding is Binding { Path: { } pp } ? pp.Path : null;

    private static void WriteBack(PropertyInfo prop, object item, object? value)
    {
        try
        {
            if (value is null)
            {
                if (!prop.PropertyType.IsValueType || Nullable.GetUnderlyingType(prop.PropertyType) is not null)
                {
                    prop.SetValue(item, null);
                }

                return;
            }

            var target = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            var set = target.IsInstanceOfType(value)
                ? value
                : Convert.ChangeType(value, target, System.Globalization.CultureInfo.CurrentCulture);
            prop.SetValue(item, set);
        }
        catch (Exception)
        {
            // Ignore write failures (type mismatch / setter throw).
        }
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
