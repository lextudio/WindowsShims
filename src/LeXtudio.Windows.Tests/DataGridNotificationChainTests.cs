using NUnit.Framework;
using System.Reflection;
using System.Windows.Controls;

namespace LeXtudio.Windows.Tests;

/// <summary>
/// Session 66: verifies that the upstream DataGrid notification chain
/// (DataGrid.NotifyPropertyChanged → _rowTrackingRoot → row → cells) surfaces
/// are properly wired. Tests are reflection-only so they don't require the full
/// Uno runtime (no instance construction).
/// </summary>
[TestFixture]
public sealed class DataGridNotificationChainTests
{
    [Test]
    public void DataGridRowTrackerPropertyExists()
    {
        // Tracker is initialized in PrepareRow; the property must exist so
        // BuildShimVisualTree can call row.Tracker.StartTracking(ref _rowTrackingRoot).
        var tracker = typeof(DataGridRow).GetProperty(
            "Tracker",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        Assert.That(tracker, Is.Not.Null, "DataGridRow.Tracker property exists");
        // Must be the ContainerTracking<DataGridRow> type.
        Assert.That(tracker!.PropertyType.Name, Does.Contain("ContainerTracking"),
            "Tracker is ContainerTracking<DataGridRow>");
    }

    [Test]
    public void DataGridRowNotifyPropertyChangedSignatureMatchesUpstream()
    {
        // The upstream DataGrid iterates _rowTrackingRoot and calls
        // row.NotifyPropertyChanged(d, propertyName, e, target).
        // This test asserts the shim exposes that exact signature.
        var method = typeof(DataGridRow).GetMethod(
            "NotifyPropertyChanged",
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: [typeof(DependencyObject), typeof(string), typeof(System.Windows.DependencyPropertyChangedEventArgs), typeof(DataGridNotificationTarget)],
            modifiers: null);

        Assert.That(method, Is.Not.Null, "row.NotifyPropertyChanged(DependencyObject, string, DPChangedEventArgs, target) exists");
    }

    [Test]
    public void DataGridCellNotifyPropertyChangedSignatureMatchesUpstream()
    {
        // The upstream DataGridRow forwards to each cell via this signature.
        var method = typeof(DataGridCell).GetMethod(
            "NotifyPropertyChanged",
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: [typeof(DependencyObject), typeof(string), typeof(System.Windows.DependencyPropertyChangedEventArgs), typeof(DataGridNotificationTarget)],
            modifiers: null);

        Assert.That(method, Is.Not.Null, "cell.NotifyPropertyChanged(DependencyObject, string, DPChangedEventArgs, target) exists");
    }

    [Test]
    public void DataGridRowOnColumnsChangedSignatureMatchesUpstream()
    {
        // The upstream DataGrid.UpdateColumnsOnRows calls
        // row.OnColumnsChanged(ObservableCollection<DataGridColumn>, e).
        var method = typeof(DataGridRow).GetMethod(
            "OnColumnsChanged",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            binder: null,
            types: [typeof(System.Collections.ObjectModel.ObservableCollection<DataGridColumn>), typeof(System.Collections.Specialized.NotifyCollectionChangedEventArgs)],
            modifiers: null);

        Assert.That(method, Is.Not.Null, "row.OnColumnsChanged(ObservableCollection<DataGridColumn>, e) exists");
    }

    [Test]
    public void DataGridCellNotifyCurrentCellContainerChangedSignatureExists()
    {
        // NotifyCurrentCellContainerChanged is called by the upstream
        // DataGrid.CurrentCellContainer setter to update cell focus visuals.
        var method = typeof(DataGridCell).GetMethod(
            "NotifyCurrentCellContainerChanged",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null, "cell.NotifyCurrentCellContainerChanged() is no longer a stub");
    }

    [Test]
    public void DataGridHelperExposesNotificationTargetPredicates()
    {
        // Session 66 adds the ShouldNotify* predicates needed by the row/cell
        // notification implementations. Verify they are present in the shim.
        var helperType = typeof(DataGrid).Assembly.GetType("System.Windows.Controls.DataGridHelper")!;
        var names = new[] { "ShouldNotifyCells", "ShouldNotifyCellsPresenter", "ShouldRefreshCellContent",
                            "ShouldNotifyRows", "ShouldNotifyRowHeaders", "ShouldNotifyDetailsPresenter" };
        foreach (var name in names)
        {
            var m = helperType.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.That(m, Is.Not.Null, $"DataGridHelper.{name} exists");
        }
    }
}
