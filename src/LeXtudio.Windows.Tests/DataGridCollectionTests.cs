using NUnit.Framework;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows.Controls;

namespace LeXtudio.Windows.Tests;

[TestFixture]
public sealed class DataGridCollectionTests
{
    [Test]
    public void DataGridShellExposesColumnsCollection()
    {
        var columns = typeof(DataGrid).GetProperty(nameof(DataGrid.Columns));

        Assert.That(columns, Is.Not.Null);
        Assert.That(columns!.PropertyType, Is.EqualTo(typeof(ObservableCollection<DataGridColumn>)));
    }

    [Test]
    public void DataGridShellKeepsInternalColumnCollection()
    {
        var internalColumns = typeof(DataGrid).GetProperty(
            "InternalColumns",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(internalColumns, Is.Not.Null);
        Assert.That(internalColumns!.PropertyType.Name, Is.EqualTo("DataGridColumnCollection"));
    }

    [Test]
    public void DataGridColumnTracksInternalOwner()
    {
        var owner = typeof(DataGridColumn).GetProperty(
            "DataGridOwner",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(owner, Is.Not.Null);
        Assert.That(owner!.PropertyType, Is.EqualTo(typeof(DataGrid)));
    }

    [Test]
    public void ColumnCollectionBodyIsReusedFromUpstream()
    {
        // Session 65: DataGridColumnCollection is now the linked upstream type
        // (display-index model, frozen columns, notification propagation), with
        // width/virtualization regions fork-guarded out. Upstream constructs via
        // a non-public ctor taking a DataGrid (it Debug.Asserts the owner rather
        // than throwing — so the old null-throw assertion no longer applies).
        var collectionType = typeof(DataGrid).Assembly.GetType("System.Windows.Controls.DataGridColumnCollection");
        Assert.That(collectionType, Is.Not.Null);

        var constructor = collectionType!.GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: [typeof(DataGrid)],
            modifiers: null);
        Assert.That(constructor, Is.Not.Null, "non-public ctor(DataGrid) is reused from upstream");

        // Display-index surface reused from upstream; the shim adds the refresh hook.
        Assert.That(collectionType.GetMethod("ColumnFromDisplayIndex", BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null);
        Assert.That(collectionType.GetMethod("RefreshDisplayIndexMap", BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null,
            "shim refresh hook bridges the Uno DP-callback gap for direct DisplayIndex sets");
    }
}
