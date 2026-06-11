using System.Globalization;
using Microsoft.UI.Xaml.Data;
using NUnit.Framework;
using WpfBinding = System.Windows.Data.Binding;
using WpfBindingMode = System.Windows.Data.BindingMode;
using WpfIValueConverter = System.Windows.Data.IValueConverter;
using WpfUpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger;

namespace LeXtudio.Windows.Tests;

[TestFixture]
public sealed class BindingTests
{
    [Test]
    public void WpfBindingStoresWpfShapedState()
    {
        var source = new object();
        var binding = new WpfBinding("Name")
        {
            Source = source,
            ElementName = "Owner",
            Mode = WpfBindingMode.TwoWay,
            UpdateSourceTrigger = WpfUpdateSourceTrigger.PropertyChanged,
            ConverterParameter = "prefix",
            FallbackValue = "fallback",
            TargetNullValue = "null",
        };

        Assert.That(binding.Path?.Path, Is.EqualTo("Name"));
        Assert.That(binding.Source, Is.SameAs(source));
        Assert.That(binding.ElementName, Is.EqualTo("Owner"));
        Assert.That(System.Windows.Data.Binding.ToWinUIMode(binding.Mode), Is.EqualTo(BindingMode.TwoWay));
        Assert.That(System.Windows.Data.Binding.ToWinUIUpdateSourceTrigger(binding.UpdateSourceTrigger), Is.EqualTo(UpdateSourceTrigger.PropertyChanged));
        Assert.That(binding.ConverterParameter, Is.EqualTo("prefix"));
        Assert.That(binding.FallbackValue, Is.EqualTo("fallback"));
        Assert.That(binding.TargetNullValue, Is.EqualTo("null"));
    }

    [Test]
    public void WpfConverterIsAdaptedToWinUIConverter()
    {
        var binding = new WpfBinding("Name")
        {
            Converter = new PrefixConverter(),
            ConverterCulture = CultureInfo.GetCultureInfo("fr-CA"),
            ConverterParameter = "value",
        };

        var converter = binding.CreateWinUIConverter();

        Assert.That(converter, Is.Not.Null);
        Assert.That(
            converter!.Convert("text", typeof(string), binding.ConverterParameter, "en-US"),
            Is.EqualTo("fr-CA:value:text"));
    }

    private sealed class PrefixConverter : WpfIValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => $"{culture.Name}:{parameter}:{value}";

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value;
    }
}
