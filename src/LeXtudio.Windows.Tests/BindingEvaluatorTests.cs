using System.Globalization;
using System.Windows.Data;
using Xunit;

namespace LeXtudio.Windows.Tests;

public sealed class BindingEvaluatorTests
{
    [Fact]
    public void EvaluatesPublicPropertyPath()
    {
        var item = new Row(new Details("metadata"));

        var value = BindingEvaluator.Evaluate(item, new Binding("RowDetails.Name"));

        Assert.Equal("metadata", value);
    }

    [Fact]
    public void EmptyAndDotPathsReturnSource()
    {
        var item = new Row(new Details("metadata"));

        Assert.Same(item, BindingEvaluator.Evaluate(item, new Binding()));
        Assert.Same(item, BindingEvaluator.Evaluate(item, new Binding(".")));
    }

    [Fact]
    public void MissingPropertyUsesFallbackValue()
    {
        var value = BindingEvaluator.Evaluate(
            new Row(null),
            new Binding("Missing") { FallbackValue = "fallback" });

        Assert.Equal("fallback", value);
    }

    [Fact]
    public void NullPropertyUsesTargetNullValue()
    {
        var value = BindingEvaluator.Evaluate(
            new Row(null),
            new Binding("RowDetails") { TargetNullValue = "null value" });

        Assert.Equal("null value", value);
    }

    [Fact]
    public void AppliesConverterAndStringFormat()
    {
        var value = BindingEvaluator.Evaluate(
            new Row(new Details("metadata")),
            new Binding("RowDetails.Name")
            {
                Converter = new UpperConverter(),
                StringFormat = "Value: {0}",
            });

        Assert.Equal("Value: METADATA", value);
    }

    [Fact]
    public void AppliesBindingToWritableProperty()
    {
        var item = new Row(new Details("metadata"));
        var target = new Target();

        BindingEvaluator.Apply(target, nameof(Target.Text), item, new Binding("RowDetails.Name"));

        Assert.Equal("metadata", target.Text);
    }

    [Fact]
    public void AppliesBindingWithTypeConversion()
    {
        var target = new Target();

        BindingEvaluator.Apply(target, nameof(Target.Count), new CountRow("42"), new Binding("Count"));

        Assert.Equal(42, target.Count);
    }

    [Fact]
    public void ApplyRequiresWritableProperty()
    {
        var target = new Target();

        Assert.Throws<InvalidOperationException>(() =>
            BindingEvaluator.Apply(target, "Missing", new Row(null), new Binding("RowDetails")));
    }

    private sealed record Row(Details? RowDetails);

    private sealed record CountRow(string Count);

    private sealed record Details(string Name);

    private sealed class Target
    {
        public string? Text { get; set; }

        public int Count { get; set; }
    }

    private sealed class UpperConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value?.ToString()?.ToUpperInvariant() ?? string.Empty;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
