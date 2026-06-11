using System.Windows.Controls;
using NUnit.Framework;
using WpfItemCollection = System.Windows.Controls.ItemCollection;

namespace LeXtudio.Windows.Tests;

[TestFixture]
public sealed class DataGridSelectedCellsTests
{
    [Test]
    public void DataGridShellExposesSelectedCellsSurface()
    {
        var selectedCells = typeof(DataGrid).GetProperty(nameof(DataGrid.SelectedCells));
        var selectedCellsChanged = typeof(DataGrid).GetEvent(nameof(DataGrid.SelectedCellsChanged));

        Assert.That(selectedCells, Is.Not.Null);
        Assert.That(selectedCells!.PropertyType, Is.EqualTo(typeof(IList<DataGridCellInfo>)));
        Assert.That(selectedCellsChanged, Is.Not.Null);
        Assert.That(selectedCellsChanged!.EventHandlerType, Is.EqualTo(typeof(SelectedCellsChangedEventHandler)));
    }

    [Test]
    public void DataGridShellExposesItemCollection()
    {
        var items = typeof(DataGrid).GetProperty(nameof(DataGrid.Items));

        Assert.That(items, Is.Not.Null);
        Assert.That(items!.PropertyType, Is.EqualTo(typeof(WpfItemCollection)));
    }

    [Test]
    public void LinkedCellCollectionTypesAreAvailable()
    {
        var assembly = typeof(DataGrid).Assembly;
        var virtualized = assembly.GetType("System.Windows.Controls.VirtualizedCellInfoCollection");
        var selected = assembly.GetType("System.Windows.Controls.SelectedCellsCollection");

        Assert.That(virtualized, Is.Not.Null);
        Assert.That(selected, Is.Not.Null);
        Assert.That(selected!.IsSubclassOf(virtualized!), Is.True);
    }

    [Test]
    public void SelectedCellsChangedEventArgsWrapsCellLists()
    {
        var added = new List<DataGridCellInfo> { default };
        var removed = new List<DataGridCellInfo>();

        var args = new SelectedCellsChangedEventArgs(added, removed);

        Assert.That(args.AddedCells, Has.Count.EqualTo(1));
        Assert.That(args.RemovedCells, Is.Empty);
        Assert.That(args.AddedCells.IsReadOnly, Is.True);
    }

    [Test]
    public void SelectedCellsChangedEventArgsRejectsNullLists()
    {
        Assert.Throws<ArgumentNullException>(
            () => _ = new SelectedCellsChangedEventArgs(null!, new List<DataGridCellInfo>()));
        Assert.Throws<ArgumentNullException>(
            () => _ = new SelectedCellsChangedEventArgs(new List<DataGridCellInfo>(), null!));
    }
}
