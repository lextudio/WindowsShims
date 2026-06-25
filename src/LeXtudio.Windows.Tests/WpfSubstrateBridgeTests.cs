using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Runtime.CompilerServices;
using NUnit.Framework;

namespace LeXtudio.Windows.Tests;

[TestFixture]
public sealed partial class WpfSubstrateBridgeTests
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
        Assert.That(typeof(ShimDataTemplate).GetProperty(nameof(ShimDataTemplate.TemplatedParentFactory)), Is.Not.Null);
        Assert.That(typeof(ShimDataTemplate).GetProperty(nameof(IWpfTemplateBridge.TargetType)), Is.Not.Null);
        Assert.That(
            typeof(IWpfTemplateBridge).GetMethod(
                nameof(IWpfTemplateBridge.LoadContent),
                new[] { typeof(object) }),
            Is.Not.Null);
        Assert.That(
            typeof(IWpfTemplateBridge).GetMethod(
                nameof(IWpfTemplateBridge.LoadContent),
                new[] { typeof(object), typeof(Microsoft.UI.Xaml.DependencyObject) }),
            Is.Not.Null);
    }

    [Test]
    public void TemplateBridgeCarriesTemplatedParent()
    {
        var parent = (Microsoft.UI.Xaml.DependencyObject)RuntimeHelpers.GetUninitializedObject(typeof(TestDependencyObject));
        Microsoft.UI.Xaml.DependencyObject? capturedParent = null;
        object? capturedDataContext = null;
        var template = new TestTemplate((dataContext, templatedParent) =>
        {
            capturedDataContext = dataContext;
            capturedParent = templatedParent;
            return null;
        });

        ((IWpfTemplateBridge)template).LoadContent("row", parent);

        Assert.That(capturedDataContext, Is.EqualTo("row"));
        Assert.That(capturedParent, Is.SameAs(parent));
    }

    [Test]
    public void ShimDataTemplateExposesTemplatedParentFactoryConstructor()
    {
        var factoryType = typeof(Func<object?, Microsoft.UI.Xaml.DependencyObject?, Microsoft.UI.Xaml.FrameworkElement?>);

        Assert.That(typeof(ShimDataTemplate).GetConstructor(new[] { factoryType }), Is.Not.Null);
    }

    [Test]
    public void WpfTemplateBindingExposesTemplatedParentCopyHelper()
    {
        var method = typeof(WpfTemplateBinding).GetMethod(
            nameof(WpfTemplateBinding.Apply),
            new[]
            {
                typeof(Microsoft.UI.Xaml.DependencyObject),
                typeof(DependencyProperty),
                typeof(Microsoft.UI.Xaml.DependencyObject),
                typeof(DependencyProperty),
            });

        Assert.That(method, Is.Not.Null);
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

    private sealed class TestTemplate : System.Windows.Controls.ControlTemplate
    {
        public TestTemplate(
            Func<object?, Microsoft.UI.Xaml.DependencyObject?, Microsoft.UI.Xaml.FrameworkElement?> factory)
            : base(typeof(TestTemplate), factory)
        {
        }
    }

    private sealed partial class TestDependencyObject : Microsoft.UI.Xaml.DependencyObject
    {
    }

}
