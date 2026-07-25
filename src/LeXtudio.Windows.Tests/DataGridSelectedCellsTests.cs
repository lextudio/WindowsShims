using System.Reflection;
using System.Windows.Controls;
using Xunit;
using WpfItemCollection = System.Windows.Controls.ItemCollection;

namespace LeXtudio.Windows.Tests;

public sealed class DataGridSelectedCellsTests
{
    [Fact]
    public void DataGridShellExposesSelectedCellsSurface()
    {
        var selectedCells = typeof(DataGrid).GetProperty(nameof(DataGrid.SelectedCells));
        var selectedCellsChanged = typeof(DataGrid).GetEvent(nameof(DataGrid.SelectedCellsChanged));

        Assert.NotNull(selectedCells);
        Assert.Equal(typeof(IList<DataGridCellInfo>), selectedCells!.PropertyType);
        Assert.NotNull(selectedCellsChanged);
        Assert.Equal(typeof(SelectedCellsChangedEventHandler), selectedCellsChanged!.EventHandlerType);
    }

    [Fact]
    public void DataGridShellExposesItemCollection()
    {
        var items = typeof(System.Windows.Controls.ItemsControl).GetProperty(
            nameof(DataGrid.Items),
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        Assert.NotNull(items);
        Assert.Equal(typeof(WpfItemCollection), items!.PropertyType);
    }

    [Fact]
    public void LinkedCellCollectionTypesAreAvailable()
    {
        var assembly = typeof(DataGrid).Assembly;
        var virtualized = assembly.GetType("System.Windows.Controls.VirtualizedCellInfoCollection");
        var selected = assembly.GetType("System.Windows.Controls.SelectedCellsCollection");

        Assert.NotNull(virtualized);
        Assert.NotNull(selected);
        Assert.True(selected!.IsSubclassOf(virtualized!));
    }

    [Fact]
    public void SelectedCellsChangedEventArgsWrapsCellLists()
    {
        var added = new List<DataGridCellInfo> { default };
        var removed = new List<DataGridCellInfo>();

        var args = new SelectedCellsChangedEventArgs(added, removed);

        Assert.Equal(1, args.AddedCells.Count);
        Assert.Empty(args.RemovedCells);
        Assert.True(args.AddedCells.IsReadOnly);
    }

    [Fact]
    public void SelectedCellsChangedEventArgsRejectsNullLists()
    {
        Assert.Throws<ArgumentNullException>(
            () => _ = new SelectedCellsChangedEventArgs(null!, new List<DataGridCellInfo>()));
        Assert.Throws<ArgumentNullException>(
            () => _ = new SelectedCellsChangedEventArgs(new List<DataGridCellInfo>(), null!));
    }
}
