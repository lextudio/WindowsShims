using System.Globalization;
using WinUIBinding = Microsoft.UI.Xaml.Data.Binding;
using WinUIBindingBase = Microsoft.UI.Xaml.Data.BindingBase;
using WinUIBindingMode = Microsoft.UI.Xaml.Data.BindingMode;
using WinUIBindingOperations = Microsoft.UI.Xaml.Data.BindingOperations;
using WinUIIValueConverter = Microsoft.UI.Xaml.Data.IValueConverter;
using WinUIPropertyPath = Microsoft.UI.Xaml.PropertyPath;
using WinUIUpdateSourceTrigger = Microsoft.UI.Xaml.Data.UpdateSourceTrigger;

namespace System.Windows.Data;

public abstract class BindingBase
{
    internal abstract WinUIBindingBase ToWinUIBindingBase();

    public virtual BindingBase Clone(BindingMode mode) => this;
}

public static class BindingOperations
{
    public static void SetBinding(DependencyObject target, DependencyProperty property, BindingBase binding)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(property);
        ArgumentNullException.ThrowIfNull(binding);

        WinUIBindingOperations.SetBinding(target, property, binding.ToWinUIBindingBase());
    }

    public static void ClearBinding(DependencyObject target, DependencyProperty property)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(property);

        target.ClearValue(property);
    }
}

public class Binding : BindingBase
{
    public Binding()
    {
    }

    public Binding(string path)
    {
        Path = new PropertyPath(path);
    }

    public PropertyPath? Path { get; set; }

    // WPF XML binding path. The selector spine checks it to pick CLR vs XML
    // evaluation; XML sources are not supported by the bridge.
    public string? XPath { get; set; }

    public object? Source { get; set; }

    public string? ElementName { get; set; }

    public IValueConverter? Converter { get; set; }

    public object? ConverterParameter { get; set; }

    public CultureInfo? ConverterCulture { get; set; }

    public object? FallbackValue { get; set; }

    public object? TargetNullValue { get; set; }

    public BindingMode Mode { get; set; } = BindingMode.Default;

    public UpdateSourceTrigger UpdateSourceTrigger { get; set; } = UpdateSourceTrigger.Default;

    public Microsoft.UI.Xaml.Data.Binding ToWinUIBinding()
    {
        var binding = new WinUIBinding();

        if (Path is not null)
        {
            binding.Path = new WinUIPropertyPath(Path.Path);
        }

        binding.Source = Source;
        binding.ElementName = ElementName;
        binding.Converter = CreateWinUIConverter();
        binding.ConverterParameter = ConverterParameter;
        binding.FallbackValue = FallbackValue;
        binding.TargetNullValue = TargetNullValue;
        binding.Mode = ToWinUIMode(Mode);
        binding.UpdateSourceTrigger = ToWinUIUpdateSourceTrigger(UpdateSourceTrigger);

        return binding;
    }

    internal override WinUIBindingBase ToWinUIBindingBase() => ToWinUIBinding();

    public static implicit operator Binding(string path) => new(path);

    internal WinUIIValueConverter? CreateWinUIConverter()
        => Converter is null ? null : new WinUIValueConverterAdapter(Converter, ConverterCulture);

    internal static WinUIBindingMode ToWinUIMode(BindingMode mode)
        => mode switch
        {
            BindingMode.OneTime => WinUIBindingMode.OneTime,
            BindingMode.OneWay => WinUIBindingMode.OneWay,
            BindingMode.TwoWay => WinUIBindingMode.TwoWay,
            BindingMode.OneWayToSource => WinUIBindingMode.TwoWay,
            _ => WinUIBindingMode.OneWay,
        };

    internal static WinUIUpdateSourceTrigger ToWinUIUpdateSourceTrigger(UpdateSourceTrigger trigger)
        => trigger switch
        {
            UpdateSourceTrigger.PropertyChanged => WinUIUpdateSourceTrigger.PropertyChanged,
            UpdateSourceTrigger.Explicit => WinUIUpdateSourceTrigger.Explicit,
            _ => WinUIUpdateSourceTrigger.Default,
        };

    private sealed class WinUIValueConverterAdapter : WinUIIValueConverter
    {
        private readonly IValueConverter _converter;
        private readonly CultureInfo? _culture;

        public WinUIValueConverterAdapter(IValueConverter converter, CultureInfo? culture)
        {
            _converter = converter;
            _culture = culture;
        }

        public object? Convert(object? value, Type targetType, object? parameter, string language)
            => _converter.Convert(value, targetType, parameter, GetCulture(language));

        public object? ConvertBack(object? value, Type targetType, object? parameter, string language)
            => _converter.ConvertBack(value, targetType, parameter, GetCulture(language));

        private CultureInfo GetCulture(string language)
        {
            if (_culture is not null)
            {
                return _culture;
            }

            if (!string.IsNullOrWhiteSpace(language))
            {
                try
                {
                    return CultureInfo.GetCultureInfo(language);
                }
                catch (CultureNotFoundException)
                {
                }
            }

            return CultureInfo.CurrentCulture;
        }
    }
}

public sealed class PropertyPath
{
    public PropertyPath(string path)
    {
        Path = path ?? string.Empty;
    }

    public string Path { get; }

    public override string ToString() => Path;

    public static implicit operator PropertyPath(string path) => new(path);
}

public enum BindingMode
{
    Default,
    TwoWay,
    OneWay,
    OneTime,
    OneWayToSource,
}

public enum UpdateSourceTrigger
{
    Default,
    PropertyChanged,
    LostFocus,
    Explicit,
}
