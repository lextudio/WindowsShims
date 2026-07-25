using System.Collections;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Xunit;
using WpfItemsControl = System.Windows.Controls.ItemsControl;
using WpfRoutedEvent = System.Windows.RoutedEvent;
using WpfSelectionChangedEventArgs = System.Windows.Controls.SelectionChangedEventArgs;

namespace LeXtudio.Windows.Tests;

public sealed class SelectorSpineTests
{
    [Fact]
    public void SelectorIsLinkedOverShimItemsControl()
    {
        Assert.True(typeof(Selector).IsAbstract);
        Assert.True(typeof(Selector).IsSubclassOf(typeof(WpfItemsControl)));

        Assert.NotNull(typeof(Selector).GetProperty(nameof(Selector.SelectedIndex)));
        Assert.NotNull(typeof(Selector).GetProperty(nameof(Selector.SelectedItem)));
        Assert.NotNull(typeof(Selector).GetProperty(nameof(Selector.SelectedValue)));
        Assert.NotNull(typeof(Selector).GetProperty(nameof(Selector.SelectedValuePath)));

        var onSelectionChanged = typeof(Selector).GetMethod(
            "OnSelectionChanged",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(onSelectionChanged);
        Assert.True(onSelectionChanged!.IsVirtual);
    }

    [Fact]
    public void MultiSelectorIsLinkedOverSelector()
    {
        Assert.True(typeof(MultiSelector).IsAbstract);
        Assert.True(typeof(MultiSelector).IsSubclassOf(typeof(Selector)));
        Assert.Equal(typeof(IList), typeof(MultiSelector).GetProperty(nameof(MultiSelector.SelectedItems))?.PropertyType);
        Assert.NotNull(typeof(MultiSelector).GetMethod(nameof(MultiSelector.SelectAll)));
        Assert.NotNull(typeof(MultiSelector).GetMethod(nameof(MultiSelector.UnselectAll)));
    }

    [Fact]
    public void SelectedItemCollectionIsLinked()
    {
        var type = typeof(Selector).Assembly.GetType("System.Windows.Controls.SelectedItemCollection");

        Assert.NotNull(type);
        Assert.True(type!.IsSubclassOf(typeof(ObservableCollection<object>)));
    }

    [Fact]
    public void DataGridShellDerivesFromMultiSelector()
    {
        Assert.True(typeof(DataGrid).IsSubclassOf(typeof(MultiSelector)));

        // Items and the item-info helpers now come from the spine.
        var items = typeof(WpfItemsControl).GetProperty(
            nameof(DataGrid.Items),
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        Assert.NotNull(items);
        Assert.Equal(typeof(WpfItemsControl), items!.DeclaringType);
    }

    [Fact]
    public void SelectionChangedEventArgsRoundTripsItems()
    {
        var removed = new object[] { new() };
        var added = new object[] { new(), new() };

        var args = new WpfSelectionChangedEventArgs(new WpfRoutedEvent(), removed, added);

        Assert.Equal(removed, args.RemovedItems);
        Assert.Equal(added, args.AddedItems);
        Assert.False(args.Handled);
    }
}
