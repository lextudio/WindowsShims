using System.Globalization;

namespace System.ComponentModel
{
    // Bridge subset of WindowsBase GroupDescription (the upstream file drags
    // SortDescriptionCollection and view plumbing).
    public abstract class GroupDescription
    {
        public abstract object? GroupNameFromItem(object? item, int level, CultureInfo culture);
    }
}

namespace System.Windows.Data
{
    // Subset used by DataGrid grouping/sort interplay: property-name grouping
    // with optional converter and comparison. Group name extraction walks the
    // dotted property path like the binding-expression bridge.
    public class PropertyGroupDescription : ComponentModel.GroupDescription
    {
        public PropertyGroupDescription()
        {
        }

        public PropertyGroupDescription(string? propertyName)
        {
            PropertyName = propertyName;
        }

        public string? PropertyName { get; set; }

        public IValueConverter? Converter { get; set; }

        public StringComparison StringComparison { get; set; } = StringComparison.Ordinal;

        public override object? GroupNameFromItem(object? item, int level, CultureInfo culture)
        {
            if (string.IsNullOrEmpty(PropertyName))
            {
                return item;
            }

            var expression = (BindingExpression)BindingExpression.CreateUntargetedBindingExpression(
                null!,
                new Binding(PropertyName));
            expression.Activate(item);
            var value = expression.Value;
            expression.Deactivate();

            return Converter is null
                ? value
                : Converter.Convert(value == DependencyProperty.UnsetValue ? null : value, typeof(object), null, culture);
        }
    }
}
