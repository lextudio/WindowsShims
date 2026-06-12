using System.Collections;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using NUnit.Framework;
using WpfItemsControl = System.Windows.Controls.ItemsControl;
using WpfRoutedEvent = System.Windows.RoutedEvent;
using WpfSelectionChangedEventArgs = System.Windows.Controls.SelectionChangedEventArgs;

namespace LeXtudio.Windows.Tests;

[TestFixture]
public sealed class SelectorSpineTests
{
    [Test]
    public void SelectorIsLinkedOverShimItemsControl()
    {
        Assert.That(typeof(Selector).IsAbstract, Is.True);
        Assert.That(typeof(Selector).IsSubclassOf(typeof(WpfItemsControl)), Is.True);

        Assert.That(typeof(Selector).GetProperty(nameof(Selector.SelectedIndex)), Is.Not.Null);
        Assert.That(typeof(Selector).GetProperty(nameof(Selector.SelectedItem)), Is.Not.Null);
        Assert.That(typeof(Selector).GetProperty(nameof(Selector.SelectedValue)), Is.Not.Null);
        Assert.That(typeof(Selector).GetProperty(nameof(Selector.SelectedValuePath)), Is.Not.Null);

        var onSelectionChanged = typeof(Selector).GetMethod(
            "OnSelectionChanged",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(onSelectionChanged, Is.Not.Null);
        Assert.That(onSelectionChanged!.IsVirtual, Is.True);
    }

    [Test]
    public void MultiSelectorIsLinkedOverSelector()
    {
        Assert.That(typeof(MultiSelector).IsAbstract, Is.True);
        Assert.That(typeof(MultiSelector).IsSubclassOf(typeof(Selector)), Is.True);
        Assert.That(typeof(MultiSelector).GetProperty(nameof(MultiSelector.SelectedItems))?.PropertyType, Is.EqualTo(typeof(IList)));
        Assert.That(typeof(MultiSelector).GetMethod(nameof(MultiSelector.SelectAll)), Is.Not.Null);
        Assert.That(typeof(MultiSelector).GetMethod(nameof(MultiSelector.UnselectAll)), Is.Not.Null);
    }

    [Test]
    public void SelectedItemCollectionIsLinked()
    {
        var type = typeof(Selector).Assembly.GetType("System.Windows.Controls.SelectedItemCollection");

        Assert.That(type, Is.Not.Null);
        Assert.That(type!.IsSubclassOf(typeof(ObservableCollection<object>)), Is.True);
    }

    [Test]
    public void DataGridShellDerivesFromMultiSelector()
    {
        Assert.That(typeof(DataGrid).IsSubclassOf(typeof(MultiSelector)), Is.True);

        // Items and the item-info helpers now come from the spine.
        Assert.That(typeof(DataGrid).GetProperty(nameof(DataGrid.Items))!.DeclaringType, Is.EqualTo(typeof(WpfItemsControl)));
    }

    [Test]
    public void SelectionChangedEventArgsRoundTripsItems()
    {
        var removed = new object[] { new() };
        var added = new object[] { new(), new() };

        var args = new WpfSelectionChangedEventArgs(new WpfRoutedEvent(), removed, added);

        Assert.That(args.RemovedItems, Is.EqualTo(removed));
        Assert.That(args.AddedItems, Is.EqualTo(added));
        Assert.That(args.Handled, Is.False);
    }
}
