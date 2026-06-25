using System.Globalization;
using System.Windows.Data;
using NUnit.Framework;

namespace LeXtudio.Windows.Tests;

[TestFixture]
public sealed class BindingEvaluatorTests
{
    [Test]
    public void EvaluatesPublicPropertyPath()
    {
        var item = new Row(new Details("metadata"));

        var value = BindingEvaluator.Evaluate(item, new Binding("RowDetails.Name"));

        Assert.That(value, Is.EqualTo("metadata"));
    }

    [Test]
    public void EmptyAndDotPathsReturnSource()
    {
        var item = new Row(new Details("metadata"));

        Assert.That(BindingEvaluator.Evaluate(item, new Binding()), Is.SameAs(item));
        Assert.That(BindingEvaluator.Evaluate(item, new Binding(".")), Is.SameAs(item));
    }

    [Test]
    public void MissingPropertyUsesFallbackValue()
    {
        var value = BindingEvaluator.Evaluate(
            new Row(null),
            new Binding("Missing") { FallbackValue = "fallback" });

        Assert.That(value, Is.EqualTo("fallback"));
    }

    [Test]
    public void NullPropertyUsesTargetNullValue()
    {
        var value = BindingEvaluator.Evaluate(
            new Row(null),
            new Binding("RowDetails") { TargetNullValue = "null value" });

        Assert.That(value, Is.EqualTo("null value"));
    }

    [Test]
    public void AppliesConverterAndStringFormat()
    {
        var value = BindingEvaluator.Evaluate(
            new Row(new Details("metadata")),
            new Binding("RowDetails.Name")
            {
                Converter = new UpperConverter(),
                StringFormat = "Value: {0}",
            });

        Assert.That(value, Is.EqualTo("Value: METADATA"));
    }

    [Test]
    public void AppliesBindingToWritableProperty()
    {
        var item = new Row(new Details("metadata"));
        var target = new Target();

        BindingEvaluator.Apply(target, nameof(Target.Text), item, new Binding("RowDetails.Name"));

        Assert.That(target.Text, Is.EqualTo("metadata"));
    }

    [Test]
    public void AppliesBindingWithTypeConversion()
    {
        var target = new Target();

        BindingEvaluator.Apply(target, nameof(Target.Count), new CountRow("42"), new Binding("Count"));

        Assert.That(target.Count, Is.EqualTo(42));
    }

    [Test]
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
