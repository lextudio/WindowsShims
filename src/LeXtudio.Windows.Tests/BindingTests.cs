using System.Globalization;
using Microsoft.UI.Xaml.Data;
using Xunit;
using WpfBinding = System.Windows.Data.Binding;
using WpfBindingMode = System.Windows.Data.BindingMode;
using WpfIValueConverter = System.Windows.Data.IValueConverter;
using WpfUpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger;

namespace LeXtudio.Windows.Tests;

public sealed class BindingTests
{
    [Fact]
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

        Assert.Equal("Name", binding.Path?.Path);
        Assert.Same(source, binding.Source);
        Assert.Equal("Owner", binding.ElementName);
        Assert.Equal(BindingMode.TwoWay, System.Windows.Data.Binding.ToWinUIMode(binding.Mode));
        Assert.Equal(UpdateSourceTrigger.PropertyChanged, System.Windows.Data.Binding.ToWinUIUpdateSourceTrigger(binding.UpdateSourceTrigger));
        Assert.Equal("prefix", binding.ConverterParameter);
        Assert.Equal("fallback", binding.FallbackValue);
        Assert.Equal("null", binding.TargetNullValue);
    }

    [Fact]
    public void WpfConverterIsAdaptedToWinUIConverter()
    {
        var binding = new WpfBinding("Name")
        {
            Converter = new PrefixConverter(),
            ConverterCulture = CultureInfo.GetCultureInfo("fr-CA"),
            ConverterParameter = "value",
        };

        var converter = binding.CreateWinUIConverter();

        Assert.NotNull(converter);
        Assert.Equal("fr-CA:value:text", converter!.Convert("text", typeof(string), binding.ConverterParameter, "en-US"));
    }

    private sealed class PrefixConverter : WpfIValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => $"{culture.Name}:{parameter}:{value}";

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value;
    }
}
