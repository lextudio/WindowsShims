using System.Windows.Controls;
using NUnit.Framework;

namespace LeXtudio.Windows.Tests;

[TestFixture]
public sealed class WpfStyleFactoryTests
{
    [Test]
    public void StyleFactorySurfaceIsAvailable()
    {
        var method = typeof(WpfStyleFactory).GetMethod(
            nameof(WpfStyleFactory.Create),
            [typeof(Type), typeof(SetterSpec[])]);

        Assert.That(method, Is.Not.Null);
    }

    [Test]
    public void SetterSpecKeepsPropertyAndValue()
    {
        var spec = WpfStyleFactory.Set(
            TestProperty,
            true);

        Assert.That(spec.Property, Is.EqualTo(TestProperty));
        Assert.That(spec.Value, Is.True);
    }

    [Test]
    public void StyleSpecKeepsTargetTypeBasedOnAndSetters()
    {
        var setter = WpfStyleFactory.Set(TestProperty, true);
        var spec = WpfStyleFactory.Style(typeof(object), setter);

        Assert.That(spec.TargetType, Is.EqualTo(typeof(object)));
        Assert.That(spec.BasedOn, Is.Null);
        Assert.That(spec.Setters, Is.EqualTo(new[] { setter }));
    }

    private static readonly Microsoft.UI.Xaml.DependencyProperty TestProperty =
        Microsoft.UI.Xaml.DependencyProperty.Register(
            "Test",
            typeof(bool),
            typeof(WpfStyleFactoryTests),
            new Microsoft.UI.Xaml.PropertyMetadata(false));
}
