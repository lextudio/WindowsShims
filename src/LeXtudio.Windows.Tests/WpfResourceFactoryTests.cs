using System.Windows.Controls;
using Xunit;

namespace LeXtudio.Windows.Tests;

public sealed class WpfResourceFactoryTests
{
    [Fact]
    public void CreateManyMaterializesKeyedResources()
    {
        var resources = WpfResourceFactory.CreateMany(
            WpfResourceSpec.Value("A", 1),
            WpfResourceSpec.Value("B", "two")).ToArray();

        Assert.Equal(2, resources.Length);
        Assert.Equal("A", resources[0].Key);
        Assert.Equal(1, resources[0].Value);
        Assert.Equal("B", resources[1].Key);
        Assert.Equal("two", resources[1].Value);
    }

    [Fact]
    public void PopulateAddsResourcesToDictionary()
    {
        var dictionary = new System.Windows.ResourceDictionary();
        var typeKey = typeof(WpfResourceFactoryTests);

        WpfResourceFactory.Populate(
            dictionary,
            WpfResourceSpec.Value("A", 1),
            WpfResourceSpec.Value("B", "two"),
            WpfResourceSpec.Value(typeKey, "typed"));

        Assert.Equal(1, dictionary["A"]);
        Assert.Equal("two", dictionary["B"]);
        Assert.Equal("typed", dictionary[typeKey]);
    }

    [Fact]
    public void FilterSpecCreatesFilterTemplate()
    {
        var value = WpfResourceSpec.FlagsFilter("Flags", typeof(AttributeTargets)).CreateValue();

        Assert.IsType<DataGridExtensions.FilterControlTemplate>(value);
        var template = (DataGridExtensions.FilterControlTemplate)value;
        Assert.Equal(DataGridExtensions.FilterKind.Flags, template.Kind);
        Assert.Equal(typeof(AttributeTargets), template.FlagsType);
    }

    [Fact]
    public void DataTemplateSpecKeepsKeyWithoutMaterializingTemplate()
    {
        var spec = WpfResourceSpec.DataTemplate("Template", (_, _) => null);

        Assert.Equal("Template", spec.Key);
    }

    [Fact]
    public void StyleSpecSurfaceIsAvailable()
    {
        var spec = WpfResourceSpec.Style("Style", WpfStyleFactory.Style(typeof(object)));

        Assert.Equal("Style", spec.Key);
    }
}
