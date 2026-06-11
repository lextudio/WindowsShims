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
    public void ColumnCollectionRejectsNullOwner()
    {
        var collectionType = typeof(DataGrid).Assembly.GetType("System.Windows.Controls.DataGridColumnCollection");
        var constructor = collectionType!.GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: [typeof(DataGrid)],
            modifiers: null);

        var exception = Assert.Throws<TargetInvocationException>(() => constructor!.Invoke([null]));

        Assert.That(exception!.InnerException, Is.TypeOf<ArgumentNullException>());
    }
}
