using Xunit;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows.Controls;

namespace LeXtudio.Windows.Tests;

public sealed class DataGridCollectionTests
{
    [Fact]
    public void DataGridShellExposesColumnsCollection()
    {
        var columns = typeof(DataGrid).GetProperty(nameof(DataGrid.Columns));

        Assert.NotNull(columns);
        Assert.Equal(typeof(ObservableCollection<DataGridColumn>), columns!.PropertyType);
    }

    [Fact]
    public void DataGridShellKeepsInternalColumnCollection()
    {
        var internalColumns = typeof(DataGrid).GetProperty(
            "InternalColumns",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(internalColumns);
        Assert.Equal("DataGridColumnCollection", internalColumns!.PropertyType.Name);
    }

    [Fact]
    public void DataGridColumnTracksInternalOwner()
    {
        var owner = typeof(DataGridColumn).GetProperty(
            "DataGridOwner",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(owner);
        Assert.Equal(typeof(DataGrid), owner!.PropertyType);
    }

    [Fact]
    public void ColumnCollectionBodyIsReusedFromUpstream()
    {
        // Session 65: DataGridColumnCollection is now the linked upstream type
        // (display-index model, frozen columns, notification propagation), with
        // width/virtualization regions fork-guarded out. Upstream constructs via
        // a non-public ctor taking a DataGrid (it Debug.Asserts the owner rather
        // than throwing — so the old null-throw assertion no longer applies).
        var collectionType = typeof(DataGrid).Assembly.GetType("System.Windows.Controls.DataGridColumnCollection");
        Assert.NotNull(collectionType);

        var constructor = collectionType!.GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: [typeof(DataGrid)],
            modifiers: null);
        Assert.NotNull(constructor);

        // Display-index surface reused from upstream; the shim adds the refresh hook.
        Assert.NotNull(collectionType.GetMethod("ColumnFromDisplayIndex", BindingFlags.Instance | BindingFlags.NonPublic));
        Assert.NotNull(collectionType.GetMethod("RefreshDisplayIndexMap", BindingFlags.Instance | BindingFlags.NonPublic));
    }
}