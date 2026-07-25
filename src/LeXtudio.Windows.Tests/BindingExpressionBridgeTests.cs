using System.Reflection;
using System.Windows.Data;
using MS.Internal.Data;
using Xunit;

namespace LeXtudio.Windows.Tests;

public sealed class BindingExpressionBridgeTests
{
    private sealed record Person(string Name, Address Home);

    private sealed record Address(string City);

    private static BindingExpression CreateUntargeted(Binding binding)
        => (BindingExpression)BindingExpression.CreateUntargetedBindingExpression(null!, binding);

    [Fact]
    public void UntargetedExpressionEvaluatesDottedPath()
    {
        var expression = CreateUntargeted(new Binding("Home.City"));

        expression.Activate(new Person("Ada", new Address("London")));

        Assert.Equal("Home.City", expression.ParentBinding.Path!.Path);
        Assert.Equal("London", expression.Value);
    }

    [Fact]
    public void UntargetedExpressionWithEmptyPathReturnsItem()
    {
        var expression = CreateUntargeted(new Binding(string.Empty));
        var item = new object();

        expression.Activate(item);

        Assert.Same(item, expression.Value);
    }

    [Fact]
    public void DeactivatedExpressionReturnsUnsetValue()
    {
        var expression = CreateUntargeted(new Binding("Name"));

        expression.Activate(new Person("Ada", new Address("London")));
        expression.Deactivate();

        Assert.Equal(BindingValue.UnsetValue, expression.Value);
    }

    [Fact]
    public void MissingPathMemberReturnsUnsetValue()
    {
        var expression = CreateUntargeted(new Binding("DoesNotExist"));

        expression.Activate(new Person("Ada", new Address("London")));

        Assert.Equal(BindingValue.UnsetValue, expression.Value);
    }

    [Fact]
    public void DynamicValueConverterConvertsCompatibleValues()
    {
        var converter = new DynamicValueConverter(false);

        Assert.Equal(42, converter.Convert("42", typeof(int)));
        Assert.Equal("42", converter.Convert(42, typeof(string)));
        Assert.Equal(42, converter.Convert(42, typeof(object)));
    }

    [Fact]
    public void DynamicValueConverterReturnsUnsetValueWhenConversionFails()
    {
        var converter = new DynamicValueConverter(false);

        Assert.Equal(BindingValue.UnsetValue, converter.Convert("not a number", typeof(int)));
        Assert.Equal(BindingValue.UnsetValue, converter.Convert(null, typeof(int)));
        Assert.Null(converter.Convert(null, typeof(string)));
    }

    [Fact]
    public void DisconnectedItemSentinelIsStable()
    {
        var first = BindingExpressionBase.DisconnectedItem;
        var second = BindingExpressionBase.DisconnectedItem;

        Assert.Same(second, first);
    }

    [Fact]
    public void ItemsControlShimCarriesSpineVirtuals()
    {
        var type = typeof(System.Windows.Controls.ItemsControl);
        string[] virtuals =
        [
            "OnInitialized",
            "OnIsKeyboardFocusWithinChanged",
            "OnItemsChanged",
            "OnItemsSourceChanged",
            "PrepareContainerForItemOverride",
            "ClearContainerForItemOverride",
            "AdjustItemInfoOverride",
        ];

        foreach (var name in virtuals)
        {
            var method = type.GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            Assert.NotNull(method);
            Assert.True(method!.IsVirtual);
        }
    }

    [Fact]
    public void BooleanBoxesAreLinkedAndCached()
    {
        var boxes = typeof(System.Windows.Controls.DataGrid).Assembly
            .GetType("MS.Internal.KnownBoxes.BooleanBoxes");

        Assert.NotNull(boxes);

        var box = boxes!.GetMethod("Box", BindingFlags.Static | BindingFlags.NonPublic, [typeof(bool)]);
        var trueBox = boxes.GetField("TrueBox", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null);

        Assert.Same(trueBox, box!.Invoke(null, [true]));
    }
}