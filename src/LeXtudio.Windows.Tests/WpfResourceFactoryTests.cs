using System.Windows.Controls;
using NUnit.Framework;

namespace LeXtudio.Windows.Tests;

[TestFixture]
public sealed class WpfResourceFactoryTests
{
    [Test]
    public void CreateManyMaterializesKeyedResources()
    {
        var resources = WpfResourceFactory.CreateMany(
            WpfResourceSpec.Value("A", 1),
            WpfResourceSpec.Value("B", "two")).ToArray();

        Assert.That(resources, Has.Length.EqualTo(2));
        Assert.That(resources[0].Key, Is.EqualTo("A"));
        Assert.That(resources[0].Value, Is.EqualTo(1));
        Assert.That(resources[1].Key, Is.EqualTo("B"));
        Assert.That(resources[1].Value, Is.EqualTo("two"));
    }

    [Test]
    public void PopulateAddsResourcesToDictionary()
    {
        var dictionary = new System.Windows.ResourceDictionary();
        var typeKey = typeof(WpfResourceFactoryTests);

        WpfResourceFactory.Populate(
            dictionary,
            WpfResourceSpec.Value("A", 1),
            WpfResourceSpec.Value("B", "two"),
            WpfResourceSpec.Value(typeKey, "typed"));

        Assert.That(dictionary["A"], Is.EqualTo(1));
        Assert.That(dictionary["B"], Is.EqualTo("two"));
        Assert.That(dictionary[typeKey], Is.EqualTo("typed"));
    }

    [Test]
    public void FilterSpecCreatesFilterTemplate()
    {
        var value = WpfResourceSpec.FlagsFilter("Flags", typeof(AttributeTargets)).CreateValue();

        Assert.That(value, Is.TypeOf<DataGridExtensions.FilterControlTemplate>());
        var template = (DataGridExtensions.FilterControlTemplate)value;
        Assert.That(template.Kind, Is.EqualTo(DataGridExtensions.FilterKind.Flags));
        Assert.That(template.FlagsType, Is.EqualTo(typeof(AttributeTargets)));
    }

    [Test]
    public void DataTemplateSpecKeepsKeyWithoutMaterializingTemplate()
    {
        var spec = WpfResourceSpec.DataTemplate("Template", (_, _) => null);

        Assert.That(spec.Key, Is.EqualTo("Template"));
    }

    [Test]
    public void StyleSpecSurfaceIsAvailable()
    {
        var spec = WpfResourceSpec.Style("Style", WpfStyleFactory.Style(typeof(object)));

        Assert.That(spec.Key, Is.EqualTo("Style"));
    }
}
