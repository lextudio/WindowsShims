using System.Reflection;
using System.Windows.Data;
using MS.Internal.Data;
using NUnit.Framework;

namespace LeXtudio.Windows.Tests;

[TestFixture]
public sealed class BindingExpressionBridgeTests
{
    private sealed record Person(string Name, Address Home);

    private sealed record Address(string City);

    private static BindingExpression CreateUntargeted(Binding binding)
        => (BindingExpression)BindingExpression.CreateUntargetedBindingExpression(null!, binding);

    [Test]
    public void UntargetedExpressionEvaluatesDottedPath()
    {
        var expression = CreateUntargeted(new Binding("Home.City"));

        expression.Activate(new Person("Ada", new Address("London")));

        Assert.That(expression.ParentBinding.Path!.Path, Is.EqualTo("Home.City"));
        Assert.That(expression.Value, Is.EqualTo("London"));
    }

    [Test]
    public void UntargetedExpressionWithEmptyPathReturnsItem()
    {
        var expression = CreateUntargeted(new Binding(string.Empty));
        var item = new object();

        expression.Activate(item);

        Assert.That(expression.Value, Is.SameAs(item));
    }

    [Test]
    public void DeactivatedExpressionReturnsUnsetValue()
    {
        var expression = CreateUntargeted(new Binding("Name"));

        expression.Activate(new Person("Ada", new Address("London")));
        expression.Deactivate();

        Assert.That(expression.Value, Is.EqualTo(BindingValue.UnsetValue));
    }

    [Test]
    public void MissingPathMemberReturnsUnsetValue()
    {
        var expression = CreateUntargeted(new Binding("DoesNotExist"));

        expression.Activate(new Person("Ada", new Address("London")));

        Assert.That(expression.Value, Is.EqualTo(BindingValue.UnsetValue));
    }

    [Test]
    public void DynamicValueConverterConvertsCompatibleValues()
    {
        var converter = new DynamicValueConverter(false);

        Assert.That(converter.Convert("42", typeof(int)), Is.EqualTo(42));
        Assert.That(converter.Convert(42, typeof(string)), Is.EqualTo("42"));
        Assert.That(converter.Convert(42, typeof(object)), Is.EqualTo(42));
    }

    [Test]
    public void DynamicValueConverterReturnsUnsetValueWhenConversionFails()
    {
        var converter = new DynamicValueConverter(false);

        Assert.That(converter.Convert("not a number", typeof(int)), Is.EqualTo(BindingValue.UnsetValue));
        Assert.That(converter.Convert(null, typeof(int)), Is.EqualTo(BindingValue.UnsetValue));
        Assert.That(converter.Convert(null, typeof(string)), Is.Null);
    }

    [Test]
    public void DisconnectedItemSentinelIsStable()
    {
        var first = BindingExpressionBase.DisconnectedItem;
        var second = BindingExpressionBase.DisconnectedItem;

        Assert.That(first, Is.SameAs(second));
    }

    [Test]
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
            var method = type.GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, name);
            Assert.That(method!.IsVirtual, Is.True, name);
        }
    }

    [Test]
    public void BooleanBoxesAreLinkedAndCached()
    {
        var boxes = typeof(System.Windows.Controls.DataGrid).Assembly
            .GetType("MS.Internal.KnownBoxes.BooleanBoxes");

        Assert.That(boxes, Is.Not.Null);

        var box = boxes!.GetMethod("Box", BindingFlags.Static | BindingFlags.NonPublic, [typeof(bool)]);
        var trueBox = boxes.GetField("TrueBox", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null);

        Assert.That(box!.Invoke(null, [true]), Is.SameAs(trueBox));
    }
}
