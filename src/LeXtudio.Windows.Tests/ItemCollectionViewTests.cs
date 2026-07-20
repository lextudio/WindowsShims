using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Data;
using NUnit.Framework;
using WpfItemCollection = System.Windows.Controls.ItemCollection;

namespace LeXtudio.Windows.Tests;

[TestFixture]
public sealed class ItemCollectionViewTests
{
    [Test]
    public void SortDescriptionsAreLinkedAndObservable()
    {
        var items = new WpfItemCollection();
        var changes = 0;
        ((INotifyCollectionChanged)items.SortDescriptions).CollectionChanged += (_, _) => changes++;

        items.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
        items.SortDescriptions.Clear();

        Assert.That(changes, Is.GreaterThanOrEqualTo(2));
        Assert.That(items.SortDescriptions, Is.Empty);
    }

    [Test]
    public void EditableViewTracksEditItem()
    {
        var items = new WpfItemCollection();
        var item = new object();
        items.Add(item);

        IEditableCollectionView view = items;
        view.EditItem(item);

        Assert.That(view.IsEditingItem, Is.True);
        Assert.That(view.CurrentEditItem, Is.SameAs(item));

        view.CommitEdit();

        Assert.That(view.IsEditingItem, Is.False);
    }

    [Test]
    public void EditableViewExposesAddNewBridge()
    {
        IEditableCollectionView view = new WpfItemCollection();

        Assert.That(view.CanAddNew, Is.True);
        Assert.That(view.CanCancelEdit, Is.False);
        Assert.That(view.NewItemPlaceholderPosition, Is.EqualTo(NewItemPlaceholderPosition.None));
        Assert.Throws<InvalidOperationException>(() => view.AddNew());

        view.NewItemPlaceholderPosition = NewItemPlaceholderPosition.AtBeginning;
        Assert.That(view.NewItemPlaceholderPosition, Is.EqualTo(NewItemPlaceholderPosition.AtBeginning));
    }

    [Test]
    public void EditableViewRemovesItems()
    {
        var items = new WpfItemCollection();
        var item = new object();
        items.Add(item);

        ((IEditableCollectionView)items).Remove(item);

        Assert.That(items, Is.Empty);
    }

    [Test]
    public void NewItemPlaceholderIsStableSentinel()
    {
        var first = CollectionView.NewItemPlaceholder;
        var second = CollectionView.NewItemPlaceholder;

        Assert.That(first, Is.SameAs(second));
        Assert.That(first.ToString(), Does.Contain("NewItemPlaceholder"));
    }

    private sealed record Person(string Country, string City, string Name);

    [Test]
    public void GroupDescriptionsAreLinkedAndObservable()
    {
        var items = new WpfItemCollection();
        var changes = 0;
        ((INotifyCollectionChanged)items.GroupDescriptions).CollectionChanged += (_, _) => changes++;

        items.GroupDescriptions.Add(new PropertyGroupDescription("Country"));
        items.GroupDescriptions.Clear();

        Assert.That(changes, Is.GreaterThanOrEqualTo(2));
        Assert.That(items.GroupDescriptions, Is.Empty);
    }

    [Test]
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

        Assert.That(items.Groups, Has.Count.EqualTo(2));
        Assert.That(items.Groups[0].Name, Is.EqualTo("US"));
        Assert.That(items.Groups[0].Items, Is.EqualTo(new object[] { us1, us2 }));
        Assert.That(items.Groups[0].ItemCount, Is.EqualTo(2));
        Assert.That(items.Groups[0].IsBottomLevel, Is.True);
        Assert.That(items.Groups[1].Name, Is.EqualTo("UK"));
        Assert.That(items.Groups[1].Items, Is.EqualTo(new object[] { uk1 }));

        // The flat backing list is reordered into group-contiguous order.
        Assert.That(items, Is.EqualTo(new object[] { us1, us2, uk1 }));
    }

    [Test]
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

        Assert.That(items.Groups, Has.Count.EqualTo(2));
        var us = items.Groups[0];
        Assert.That(us.Name, Is.EqualTo("US"));
        Assert.That(us.IsBottomLevel, Is.False);
        Assert.That(us.ItemCount, Is.EqualTo(2));
        Assert.That(us.Items, Has.Count.EqualTo(2)); // two City subgroups, not two leaves

        var uk = items.Groups[1];
        Assert.That(uk.ItemCount, Is.EqualTo(1));
    }

    [Test]
    public void ClearingGroupDescriptionsRestoresFlatOrderOnRefresh()
    {
        var items = new WpfItemCollection();
        var us = new Person("US", "Seattle", "Alice");
        var uk = new Person("UK", "London", "Bob");
        items.Add(us);
        items.Add(uk);
        items.GroupDescriptions.Add(new PropertyGroupDescription("Country"));
        items.Refresh();
        Assert.That(items.Groups, Is.Not.Empty);

        items.GroupDescriptions.Clear();
        items.Refresh();

        Assert.That(items.Groups, Is.Empty);
        Assert.That(items, Is.EqualTo(new object[] { us, uk }));
    }

    [Test]
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

        Assert.That(items.Groups, Has.Count.EqualTo(1));
        Assert.That(items.Groups[0].Items, Is.EqualTo(new object[] { us1, us2 }));
    }

    [Test]
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

        Assert.That(slots, Has.Count.EqualTo(4)); // 2 headers + 2 leaves
        Assert.That(slots[0], Is.InstanceOf<MS.Internal.Data.GroupHeaderSlot>());
        Assert.That(((MS.Internal.Data.GroupHeaderSlot)slots[0]!).Group, Is.SameAs(items.Groups[0]));
        Assert.That(((MS.Internal.Data.GroupHeaderSlot)slots[0]!).Depth, Is.EqualTo(0));
        Assert.That(slots[1], Is.SameAs(us));
        Assert.That(slots[2], Is.InstanceOf<MS.Internal.Data.GroupHeaderSlot>());
        Assert.That(slots[3], Is.SameAs(uk));
    }

    [Test]
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
        Assert.That(slots, Has.Count.EqualTo(3));
        Assert.That(((MS.Internal.Data.GroupHeaderSlot)slots[0]!).Depth, Is.EqualTo(0));
        Assert.That(((MS.Internal.Data.GroupHeaderSlot)slots[1]!).Depth, Is.EqualTo(1));
        Assert.That(slots[2], Is.SameAs(seattle));
    }

    [Test]
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
        Assert.That(slots, Has.Count.EqualTo(3));
        Assert.That(slots[0], Is.InstanceOf<MS.Internal.Data.GroupHeaderSlot>());
        Assert.That(((MS.Internal.Data.GroupHeaderSlot)slots[0]!).Group, Is.SameAs(items.Groups[0]));
        Assert.That(slots[1], Is.InstanceOf<MS.Internal.Data.GroupHeaderSlot>());
        Assert.That(slots[2], Is.SameAs(uk));
    }

    [Test]
    public void CollapsedGroupSlotCountIsOne()
    {
        var items = new WpfItemCollection();
        items.Add(new Person("US", "Seattle", "Alice"));
        items.Add(new Person("US", "Austin", "Bob"));
        items.GroupDescriptions.Add(new PropertyGroupDescription("Country"));
        items.Refresh();

        var group = items.Groups[0];
        Assert.That(group.SlotCount, Is.EqualTo(3)); // header + 2 leaves, expanded

        group.IsExpanded = false;
        Assert.That(group.SlotCount, Is.EqualTo(1)); // header only
    }

    [Test]
    public void SlotIndexFromItemReturnsMinusOneInsideCollapsedGroup()
    {
        var items = new WpfItemCollection();
        var alice = new Person("US", "Seattle", "Alice");
        items.Add(alice);
        items.GroupDescriptions.Add(new PropertyGroupDescription("Country"));
        items.Refresh();

        var group = items.Groups[0];
        Assert.That(group.SlotIndexFromItem(alice, 0), Is.EqualTo(1)); // header at 0, leaf at 1

        group.IsExpanded = false;
        Assert.That(group.SlotIndexFromItem(alice, 0), Is.EqualTo(-1));
    }
}
