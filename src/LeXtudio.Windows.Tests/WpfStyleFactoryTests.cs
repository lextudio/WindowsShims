using System.Windows.Controls;
using Xunit;

namespace LeXtudio.Windows.Tests;

public sealed class WpfStyleFactoryTests
{
    [Fact]
    public void StyleFactorySurfaceIsAvailable()
    {
        var method = typeof(WpfStyleFactory).GetMethod(
            nameof(WpfStyleFactory.Create),
            [typeof(Type), typeof(SetterSpec[])]);

        Assert.NotNull(method);
    }

    [Fact]
    public void SetterSpecKeepsPropertyAndValue()
    {
        var spec = WpfStyleFactory.Set(
            TestProperty,
            true);

        Assert.Equal(TestProperty, spec.Property);
        Assert.Equal(true, spec.Value);
    }

    [Fact]
    public void StyleSpecKeepsTargetTypeBasedOnAndSetters()
    {
        var setter = WpfStyleFactory.Set(TestProperty, true);
        var spec = WpfStyleFactory.Style(typeof(object), setter);

        Assert.Equal(typeof(object), spec.TargetType);
        Assert.Null(spec.BasedOn);
        Assert.Equal(new[] { setter }, spec.Setters);
    }

    private static readonly Microsoft.UI.Xaml.DependencyProperty TestProperty =
        Microsoft.UI.Xaml.DependencyProperty.Register(
            "Test",
            typeof(bool),
            typeof(WpfStyleFactoryTests),
            new Microsoft.UI.Xaml.PropertyMetadata(false));
}
