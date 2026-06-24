using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using NUnit.Framework;

namespace LeXtudio.Windows.Tests;

[TestFixture]
public sealed class WpfSubstrateBridgeTests
{
    [Test]
    public void RelativeSourceCarriesFindAncestorState()
    {
        var source = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(DataGrid), 2);
        var binding = new Binding("DataContext.Filter")
        {
            RelativeSource = source,
        };

        Assert.That(binding.RelativeSource, Is.SameAs(source));
        Assert.That(source.Mode, Is.EqualTo(RelativeSourceMode.FindAncestor));
        Assert.That(source.AncestorType, Is.EqualTo(typeof(DataGrid)));
        Assert.That(source.AncestorLevel, Is.EqualTo(2));
    }

    [Test]
    public void RelativeSourceProvidesWpfSingletons()
    {
        Assert.That(RelativeSource.Self.Mode, Is.EqualTo(RelativeSourceMode.Self));
        Assert.That(RelativeSource.TemplatedParent.Mode, Is.EqualTo(RelativeSourceMode.TemplatedParent));
        Assert.That(RelativeSource.PreviousData.Mode, Is.EqualTo(RelativeSourceMode.PreviousData));
    }

    [Test]
    public void ResourceExtensionsCarryObjectKeys()
    {
#if WINDOWS_APP_SDK
        var componentKey = new ComponentResourceKey(typeof(DataGrid), "FocusVisual");

        Assert.That(new StaticResourceExtension(typeof(DataGridCell)).ResourceKey, Is.EqualTo(typeof(DataGridCell)));
        Assert.That(new DynamicResourceExtension(componentKey).ResourceKey, Is.EqualTo(componentKey));
#else
        Assert.Pass("Desktop target uses Uno.Xaml resource extensions.");
#endif
    }

    [Test]
    public void TemplateBindingExtensionProducesExpression()
    {
#if WINDOWS_APP_SDK
        var extension = new TemplateBindingExtension(Microsoft.UI.Xaml.Controls.Control.BackgroundProperty);
        var value = extension.ProvideValue(TestServiceProvider.Instance);

        Assert.That(extension.Property, Is.EqualTo(Microsoft.UI.Xaml.Controls.Control.BackgroundProperty));
        Assert.That(value, Is.TypeOf<TemplateBindingExpression>());
        Assert.That(((TemplateBindingExpression)value).TemplateBindingExtension, Is.SameAs(extension));
#else
        Assert.Pass("Desktop target uses Uno.Xaml template-binding extensions.");
#endif
    }

    [Test]
    public void ShimDataTemplateImplementsGenericTemplateBridge()
    {
        Assert.That(typeof(IWpfTemplateBridge).IsAssignableFrom(typeof(ShimDataTemplate)), Is.True);
        Assert.That(typeof(ShimDataTemplate).GetProperty(nameof(ShimDataTemplate.Factory)), Is.Not.Null);
        Assert.That(typeof(ShimDataTemplate).GetProperty(nameof(IWpfTemplateBridge.TargetType)), Is.Not.Null);
        Assert.That(
            typeof(IWpfTemplateBridge).GetMethod(nameof(IWpfTemplateBridge.LoadContent)),
            Is.Not.Null);
    }

    [Test]
    public void ControlTemplateCarriesTargetTypeThroughBridge()
    {
        var template = new DataGridExtensions.FilterControlTemplate(DataGridExtensions.FilterKind.Hex);
        var bridge = (IWpfTemplateBridge)template;

        Assert.That(bridge.TargetType, Is.EqualTo(typeof(System.Windows.Controls.Primitives.DataGridColumnHeader)));
        Assert.That(template.Kind, Is.EqualTo(DataGridExtensions.FilterKind.Hex));
    }

    private sealed class TestServiceProvider : IServiceProvider
    {
        public static readonly TestServiceProvider Instance = new();

        public object? GetService(Type serviceType) => null;
    }
}
