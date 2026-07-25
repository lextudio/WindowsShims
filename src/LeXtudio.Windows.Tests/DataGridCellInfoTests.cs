using System.Windows.Controls;
using Xunit;
using ItemInfo = System.Windows.Controls.ItemsControl.ItemInfo;

namespace LeXtudio.Windows.Tests;

public sealed class DataGridCellInfoTests
{
    [Fact]
    public void CellInfoRequiresColumn()
    {
        Assert.Throws<ArgumentNullException>(() => _ = new DataGridCellInfo(new object(), null!));
    }

    [Fact]
    public void CellInfoProvidesExpectedSurface()
    {
        Assert.True(typeof(DataGridCellInfo).IsValueType);
        Assert.NotNull(typeof(DataGridCellInfo).GetProperty(nameof(DataGridCellInfo.Item)));
        Assert.NotNull(typeof(DataGridCellInfo).GetProperty(nameof(DataGridCellInfo.Column)));
        Assert.NotNull(typeof(DataGridCellInfo).GetProperty(nameof(DataGridCellInfo.IsValid)));
    }

    [Fact]
    public void ItemInfoBridgeComparesByItem()
    {
        var item = new object();
        var first = new ItemInfo(item);
        var second = new ItemInfo(item);
        var other = new ItemInfo(new object());

        Assert.True(first == second);
        Assert.False(first == other);
        Assert.False(first == null);
        Assert.Equal(second.GetHashCode(), first.GetHashCode());
    }

    [Fact]
    public void ItemInfoBridgeHonorsIndexMismatch()
    {
        var item = new object();
        var unindexed = new ItemInfo(item);
        var indexed = new ItemInfo(item, container: null, index: 2);
        var otherIndex = new ItemInfo(item, container: null, index: 5);

        Assert.True(unindexed == indexed);
        Assert.False(indexed == otherIndex);
    }

    [Fact]
    public void ItemInfoBridgeClonesState()
    {
        var item = new object();
        var info = new ItemInfo(item, container: null, index: 3);

        var clone = info.Clone();

        Assert.NotSame(info, clone);
        Assert.Same(item, clone.Item);
        Assert.Equal(3, clone.Index);
        Assert.True(clone == info);
    }

    [Fact]
    public void NewItemEventArgsRoundTrip()
    {
        var adding = new AddingNewItemEventArgs();
        var newItem = new object();
        adding.NewItem = newItem;

        var initializing = new InitializingNewItemEventArgs(newItem);

        Assert.Same(newItem, adding.NewItem);
        Assert.Same(newItem, initializing.NewItem);
    }

    [Fact]
    public void InitializingNewItemHandlerDelegateIsAvailable()
    {
        var invoke = typeof(InitializingNewItemEventHandler).GetMethod("Invoke");

        Assert.NotNull(invoke);
        Assert.Equal(typeof(InitializingNewItemEventArgs), invoke!.GetParameters()[1].ParameterType);
    }
}
