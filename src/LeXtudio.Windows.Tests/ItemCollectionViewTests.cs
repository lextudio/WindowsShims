using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Data;
using Xunit;
using WpfItemCollection = System.Windows.Controls.ItemCollection;

namespace LeXtudio.Windows.Tests;

public sealed class ItemCollectionViewTests
{
    [Fact]
    public void SortDescriptionsAreLinkedAndObservable()
    {
        var items = new WpfItemCollection();
        var changes = 0;
        ((INotifyCollectionChanged)items.SortDescriptions).CollectionChanged += (_, _) => changes++;

        items.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
        items.SortDescriptions.Clear();

        Assert.True(changes >= 2);
        Assert.Empty(items.SortDescriptions);
    }

    [Fact]
    public void EditableViewTracksEditItem()
    {
        var items = new WpfItemCollection();
        var item = new object();
        items.Add(item);

        IEditableCollectionView view = items;
        view.EditItem(item);

        Assert.True(view.IsEditingItem);
        Assert.Same(item, view.CurrentEditItem);

        view.CommitEdit();

        Assert.False(view.IsEditingItem);
    }

    [Fact]
    public void EditableViewExposesAddNewBridge()
    {
        IEditableCollectionView view = new WpfItemCollection();

        Assert.True(view.CanAddNew);
        Assert.False(view.CanCancelEdit);
        Assert.Equal(NewItemPlaceholderPosition.None, view.NewItemPlaceholderPosition);
        Assert.Throws<InvalidOperationException>(() => view.AddNew());

        view.NewItemPlaceholderPosition = NewItemPlaceholderPosition.AtBeginning;
        Assert.Equal(NewItemPlaceholderPosition.AtBeginning, view.NewItemPlaceholderPosition);
    }

    [Fact]
    public void EditableViewRemovesItems()
    {
        var items = new WpfItemCollection();
        var item = new object();
        items.Add(item);

        ((IEditableCollectionView)items).Remove(item);

        Assert.Empty(items);
    }

    [Fact]
    public void NewItemPlaceholderIsStableSentinel()
    {
        var first = CollectionView.NewItemPlaceholder;
        var second = CollectionView.NewItemPlaceholder;

        Assert.Same(second, first);
        Assert.Contains("NewItemPlaceholder", first.ToString());
    }

    private sealed record Person(string Country, string City, string Name);

    [Fact]
    public void GroupDescriptionsAreLinkedAndObservable()
    {
        var items = new WpfItemCollection();
        var changes = 0;
        ((INotifyCollectionChanged)items.GroupDescriptions).CollectionChanged += (_, _) => changes++;

        items.GroupDescriptions.Add(new PropertyGroupDescription("Country"));
        items.GroupDescriptions.Clear();

        Assert.True(changes >= 2);
        Assert.Empty(items.GroupDescriptions);
    }

    [Fact]
    public void SingleLevelGroupingBucketsByFirstEncounterOrder()
    {
        var items = new WpfItemCollection();
        var us1 = new Person("US", "Seattle", "Alice");
        var uk1 = new Person("UK", "London", "Bob");
        var us2 = new Person("US", "Austin", "Carol");

        items.Add(us1);
        items.Add(uk1);
        items.Add(us2);
        items.GroupDescriptions.Add(new PropertyGroupDescription("Country"));
        items.Refresh();

        Assert.Equal(2, items.Groups.Count);
        Assert.Equal("US", items.Groups[0].Name);
        Assert.Equal(new object[] { us1, us2 }, items.Groups[0].Items);
        Assert.Equal(2, items.Groups[0].ItemCount);
        Assert.True(items.Groups[0].IsBottomLevel);
        Assert.Equal("UK", items.Groups[1].Name);
        Assert.Equal(new object[] { uk1 }, items.Groups[1].Items);

        // The flat backing list is reordered into group-contiguous order.
        Assert.Equal(new object[] { us1, us2, uk1 }, items);
    }

    [Fact]
    public void MultiLevelGroupingNestsSubgroups()
    {
        var items = new WpfItemCollection();
        var seattle = new Person("US", "Seattle", "Alice");
        var austin = new Person("US", "Austin", "Bob");
        var london = new Person("UK", "London", "Carol");

        items.Add(seattle);
        items.Add(austin);
        items.Add(london);
        items.GroupDescriptions.Add(new PropertyGroupDescription("Country"));
        items.GroupDescriptions.Add(new PropertyGroupDescription("City"));
        items.Refresh();

        Assert.Equal(2, items.Groups.Count);
        var us = items.Groups[0];
        Assert.Equal("US", us.Name);
        Assert.False(us.IsBottomLevel);
        Assert.Equal(2, us.ItemCount);
        Assert.Equal(2, us.Items.Count); // two City subgroups, not two leaves

        var uk = items.Groups[1];
        Assert.Equal(1, uk.ItemCount);
    }

    [Fact]
    public void ClearingGroupDescriptionsRestoresFlatOrderOnRefresh()
    {
        var items = new WpfItemCollection();
        var us = new Person("US", "Seattle", "Alice");
        var uk = new Person("UK", "London", "Bob");
        items.Add(us);
        items.Add(uk);
        items.GroupDescriptions.Add(new PropertyGroupDescription("Country"));
        items.Refresh();
        Assert.NotEmpty(items.Groups);

        items.GroupDescriptions.Clear();
        items.Refresh();

        Assert.Empty(items.Groups);
        Assert.Equal(new object[] { us, uk }, items);
    }

    [Fact]
    public void GroupingComposesWithSortDescriptions()
    {
        var items = new WpfItemCollection();
        var us2 = new Person("US", "Seattle", "Zed");
        var us1 = new Person("US", "Seattle", "Amy");
        items.Add(us2);
        items.Add(us1);
        items.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
        items.GroupDescriptions.Add(new PropertyGroupDescription("Country"));
        items.Refresh();

        Assert.Equal(1, items.Groups.Count);
        Assert.Equal(new object[] { us1, us2 }, items.Groups[0].Items);
    }

    [Fact]
    public void FlattenWithHeadersInterleavesHeaderSlotsAheadOfEachGroupSingleLevel()
    {
        var items = new WpfItemCollection();
        var us = new Person("US", "Seattle", "Alice");
        var uk = new Person("UK", "London", "Bob");
        items.Add(us);
        items.Add(uk);
        items.GroupDescriptions.Add(new PropertyGroupDescription("Country"));
        items.Refresh();

        var slots = MS.Internal.Data.CollectionViewGroupBuilder.FlattenWithHeaders(items.Groups);

        Assert.Equal(4, slots.Count); // 2 headers + 2 leaves
        Assert.IsAssignableFrom<MS.Internal.Data.GroupHeaderSlot>(slots[0]);
        Assert.Same(items.Groups[0], ((MS.Internal.Data.GroupHeaderSlot)slots[0]!).Group);
        Assert.Equal(0, ((MS.Internal.Data.GroupHeaderSlot)slots[0]!).Depth);
        Assert.Same(us, slots[1]);
        Assert.IsAssignableFrom<MS.Internal.Data.GroupHeaderSlot>(slots[2]);
        Assert.Same(uk, slots[3]);
    }

    [Fact]
    public void FlattenWithHeadersIncreasesDepthForNestedSubgroups()
    {
        var items = new WpfItemCollection();
        var seattle = new Person("US", "Seattle", "Alice");
        items.Add(seattle);
        items.GroupDescriptions.Add(new PropertyGroupDescription("Country"));
        items.GroupDescriptions.Add(new PropertyGroupDescription("City"));
        items.Refresh();

        var slots = MS.Internal.Data.CollectionViewGroupBuilder.FlattenWithHeaders(items.Groups);

        // Country header (depth 0), City header (depth 1), leaf item.
        Assert.Equal(3, slots.Count);
        Assert.Equal(0, ((MS.Internal.Data.GroupHeaderSlot)slots[0]!).Depth);
        Assert.Equal(1, ((MS.Internal.Data.GroupHeaderSlot)slots[1]!).Depth);
        Assert.Same(seattle, slots[2]);
    }

    [Fact]
    public void CollapsedGroupExcludesChildrenFromFlattenWithHeaders()
    {
        var items = new WpfItemCollection();
        var us = new Person("US", "Seattle", "Alice");
        var uk = new Person("UK", "London", "Bob");
        items.Add(us);
        items.Add(uk);
        items.GroupDescriptions.Add(new PropertyGroupDescription("Country"));
        items.Refresh();

        items.Groups[0].IsExpanded = false;
        var slots = MS.Internal.Data.CollectionViewGroupBuilder.FlattenWithHeaders(items.Groups);

        // Collapsed US group: header only, no leaf. UK group: header + leaf, unaffected.
        Assert.Equal(3, slots.Count);
        Assert.IsAssignableFrom<MS.Internal.Data.GroupHeaderSlot>(slots[0]);
        Assert.Same(items.Groups[0], ((MS.Internal.Data.GroupHeaderSlot)slots[0]!).Group);
        Assert.IsAssignableFrom<MS.Internal.Data.GroupHeaderSlot>(slots[1]);
        Assert.Same(uk, slots[2]);
    }

    [Fact]
    public void CollapsedGroupSlotCountIsOne()
    {
        var items = new WpfItemCollection();
        items.Add(new Person("US", "Seattle", "Alice"));
        items.Add(new Person("US", "Austin", "Bob"));
        items.GroupDescriptions.Add(new PropertyGroupDescription("Country"));
        items.Refresh();

        var group = items.Groups[0];
        Assert.Equal(3, group.SlotCount); // header + 2 leaves, expanded

        group.IsExpanded = false;
        Assert.Equal(1, group.SlotCount); // header only
    }

    [Fact]
    public void SlotIndexFromItemReturnsMinusOneInsideCollapsedGroup()
    {
        var items = new WpfItemCollection();
        var alice = new Person("US", "Seattle", "Alice");
        items.Add(alice);
        items.GroupDescriptions.Add(new PropertyGroupDescription("Country"));
        items.Refresh();

        var group = items.Groups[0];
        Assert.Equal(1, group.SlotIndexFromItem(alice, 0)); // header at 0, leaf at 1

        group.IsExpanded = false;
        Assert.Equal(-1, group.SlotIndexFromItem(alice, 0));
    }
}
