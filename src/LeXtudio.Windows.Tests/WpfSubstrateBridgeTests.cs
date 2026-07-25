using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Runtime.CompilerServices;
using Xunit;

namespace LeXtudio.Windows.Tests;

public sealed partial class WpfSubstrateBridgeTests
{
    [Fact]
    public void RelativeSourceCarriesFindAncestorState()
    {
        var source = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(DataGrid), 2);
        var binding = new Binding("DataContext.Filter")
        {
            RelativeSource = source,
        };

        Assert.Same(source, binding.RelativeSource);
        Assert.Equal(RelativeSourceMode.FindAncestor, source.Mode);
        Assert.Equal(typeof(DataGrid), source.AncestorType);
        Assert.Equal(2, source.AncestorLevel);
    }

    [Fact]
    public void RelativeSourceProvidesWpfSingletons()
    {
        Assert.Equal(RelativeSourceMode.Self, RelativeSource.Self.Mode);
        Assert.Equal(RelativeSourceMode.TemplatedParent, RelativeSource.TemplatedParent.Mode);
        Assert.Equal(RelativeSourceMode.PreviousData, RelativeSource.PreviousData.Mode);
    }

    [Fact]
    public void ResourceExtensionsCarryObjectKeys()
    {
#if WINDOWS_APP_SDK
        var componentKey = new ComponentResourceKey(typeof(DataGrid), "FocusVisual");

        Assert.Equal(typeof(DataGridCell), new StaticResourceExtension(typeof(DataGridCell)).ResourceKey);
        Assert.Equal(componentKey, new DynamicResourceExtension(componentKey).ResourceKey);
#else
        // Assert.Pass("Desktop target uses Uno.Xaml resource extensions.");
#endif
    }

    [Fact]
    public void TemplateBindingExtensionProducesExpression()
    {
#if WINDOWS_APP_SDK
        var extension = new TemplateBindingExtension(Microsoft.UI.Xaml.Controls.Control.BackgroundProperty);
        var value = extension.ProvideValue(TestServiceProvider.Instance);

        Assert.Equal(Microsoft.UI.Xaml.Controls.Control.BackgroundProperty, extension.Property);
        Assert.IsType<TemplateBindingExpression>(value);
        Assert.Same(extension, ((TemplateBindingExpression)value).TemplateBindingExtension);
#else
        // Assert.Pass("Desktop target uses Uno.Xaml template-binding extensions.");
#endif
    }

    [Fact]
    public void ShimDataTemplateImplementsGenericTemplateBridge()
    {
        Assert.True(typeof(IWpfTemplateBridge).IsAssignableFrom(typeof(ShimDataTemplate)));
        Assert.NotNull(typeof(ShimDataTemplate).GetProperty(nameof(ShimDataTemplate.Factory)));
        Assert.NotNull(typeof(ShimDataTemplate).GetProperty(nameof(ShimDataTemplate.TemplatedParentFactory)));
        Assert.NotNull(typeof(ShimDataTemplate).GetProperty(nameof(IWpfTemplateBridge.TargetType)));
        Assert.NotNull(typeof(IWpfTemplateBridge).GetMethod( nameof(IWpfTemplateBridge.LoadContent), new[] { typeof(object) }));
        Assert.NotNull(typeof(IWpfTemplateBridge).GetMethod( nameof(IWpfTemplateBridge.LoadContent), new[] { typeof(object), typeof(Microsoft.UI.Xaml.DependencyObject) }));
    }

    [Fact]
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

        Assert.Equal("row", capturedDataContext);
        Assert.Same(parent, capturedParent);
    }

    [Fact]
    public void ShimDataTemplateExposesTemplatedParentFactoryConstructor()
    {
        var factoryType = typeof(Func<object?, Microsoft.UI.Xaml.DependencyObject?, Microsoft.UI.Xaml.FrameworkElement?>);

        Assert.NotNull(typeof(ShimDataTemplate).GetConstructor(new[] { factoryType }));
    }

    [Fact]
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

        Assert.NotNull(method);
    }

    [Fact]
    public void ControlTemplateCarriesTargetTypeThroughBridge()
    {
        var template = new DataGridExtensions.FilterControlTemplate(DataGridExtensions.FilterKind.Hex);
        var bridge = (IWpfTemplateBridge)template;

        Assert.Equal(typeof(System.Windows.Controls.Primitives.DataGridColumnHeader), bridge.TargetType);
        Assert.Equal(DataGridExtensions.FilterKind.Hex, template.Kind);
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
