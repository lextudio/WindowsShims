using System.Windows.Controls;
using NUnit.Framework;
using ItemInfo = System.Windows.Controls.ItemsControl.ItemInfo;

namespace LeXtudio.Windows.Tests;

[TestFixture]
public sealed class DataGridCellInfoTests
{
    [Test]
    public void CellInfoRequiresColumn()
    {
        Assert.Throws<ArgumentNullException>(() => _ = new DataGridCellInfo(new object(), null!));
    }

    [Test]
    public void CellInfoProvidesExpectedSurface()
    {
        Assert.That(typeof(DataGridCellInfo).IsValueType, Is.True);
        Assert.That(typeof(DataGridCellInfo).GetProperty(nameof(DataGridCellInfo.Item)), Is.Not.Null);
        Assert.That(typeof(DataGridCellInfo).GetProperty(nameof(DataGridCellInfo.Column)), Is.Not.Null);
        Assert.That(typeof(DataGridCellInfo).GetProperty(nameof(DataGridCellInfo.IsValid)), Is.Not.Null);
    }

    [Test]
    public void ItemInfoBridgeComparesByItem()
    {
        var item = new object();
        var first = new ItemInfo(item);
        var second = new ItemInfo(item);
        var other = new ItemInfo(new object());

        Assert.That(first == second, Is.True);
        Assert.That(first == other, Is.False);
        Assert.That(first == null, Is.False);
        Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
    }

    [Test]
    public void ItemInfoBridgeHonorsIndexMismatch()
    {
        var item = new object();
        var unindexed = new ItemInfo(item);
        var indexed = new ItemInfo(item, container: null, index: 2);
        var otherIndex = new ItemInfo(item, container: null, index: 5);

        Assert.That(unindexed == indexed, Is.True);
        Assert.That(indexed == otherIndex, Is.False);
    }

    [Test]
    public void ItemInfoBridgeClonesState()
    {
        var item = new object();
        var info = new ItemInfo(item, container: null, index: 3);

        var clone = info.Clone();

        Assert.That(clone, Is.Not.SameAs(info));
        Assert.That(clone.Item, Is.SameAs(item));
        Assert.That(clone.Index, Is.EqualTo(3));
        Assert.That(clone == info, Is.True);
    }

    [Test]
    public void NewItemEventArgsRoundTrip()
    {
        var adding = new AddingNewItemEventArgs();
        var newItem = new object();
        adding.NewItem = newItem;

        var initializing = new InitializingNewItemEventArgs(newItem);

        Assert.That(adding.NewItem, Is.SameAs(newItem));
        Assert.That(initializing.NewItem, Is.SameAs(newItem));
    }

    [Test]
    public void InitializingNewItemHandlerDelegateIsAvailable()
    {
        var invoke = typeof(InitializingNewItemEventHandler).GetMethod("Invoke");

        Assert.That(invoke, Is.Not.Null);
        Assert.That(invoke!.GetParameters()[1].ParameterType, Is.EqualTo(typeof(InitializingNewItemEventArgs)));
    }
}
