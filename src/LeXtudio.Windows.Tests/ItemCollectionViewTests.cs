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
}
