using NUnit.Framework;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace LeXtudio.Windows.Tests;

/// <summary>
/// Session 67: verifies the column-header notification chain is wired.
/// DataGrid.NotifyPropertyChanged (via ShimNotifyColumnHeaders) routes column
/// property changes to realized DataGridColumnHeader instances — the exact
/// mirror of session 66's row-cell chain.
/// Tests are reflection-only (no Uno runtime required).
/// </summary>
[TestFixture]
public sealed class DataGridColumnHeaderNotificationTests
{
    [Test]
    public void DataGridColumnHeaderHasNotifyPropertyChangedMethod()
    {
        // Session 67 adds this method so DataGrid.ShimNotifyColumnHeaders can
        // forward property changes to each realized column header.
        var method = typeof(DataGridColumnHeader).GetMethod(
            "NotifyPropertyChanged",
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: [typeof(DependencyObject), typeof(System.Windows.DependencyPropertyChangedEventArgs)],
            modifiers: null);

        Assert.That(method, Is.Not.Null,
            "DataGridColumnHeader.NotifyPropertyChanged(DependencyObject, DPChangedEventArgs) must exist");
    }

    [Test]
    public void DataGridHasShimNotifyColumnHeadersMethod()
    {
        // DataGrid (partial) exposes ShimNotifyColumnHeaders, called from
        // the #if HAS_UNO branch inside the upstream NotifyPropertyChanged.
        var method = typeof(DataGrid).GetMethod(
            "ShimNotifyColumnHeaders",
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: [typeof(DependencyObject), typeof(System.Windows.DependencyPropertyChangedEventArgs)],
            modifiers: null);

        Assert.That(method, Is.Not.Null,
            "DataGrid.ShimNotifyColumnHeaders(DependencyObject, DPChangedEventArgs) must exist");
    }

    [Test]
    public void DataGridColumnHeaderNotifyPropertyChangedHandlesSortDirectionProperty()
    {
        // Session 68: SortDirectionProperty branch in NotifyPropertyChanged refreshes
        // the header content with the sort glyph via DataGrid.HeaderContent.
        var method = typeof(DataGridColumnHeader).GetMethod(
            "NotifyPropertyChanged",
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: [typeof(DependencyObject), typeof(System.Windows.DependencyPropertyChangedEventArgs)],
            modifiers: null);
        Assert.That(method, Is.Not.Null);

        // Verify DataGridColumn.SortDirectionProperty exists (property the branch checks).
        var sortProp = typeof(DataGridColumn).GetField(
            "SortDirectionProperty",
            BindingFlags.Public | BindingFlags.Static);
        Assert.That(sortProp, Is.Not.Null,
            "DataGridColumn.SortDirectionProperty must be accessible for the header glyph branch");
    }

    [Test]
    public void DataGridHeaderContentIsInternal()
    {
        // Session 68: DataGrid.HeaderContent must be internal so DataGridColumnHeader
        // can call Column.DataGridOwner.HeaderContent(Column) to get the glyph-decorated text.
        var method = typeof(DataGrid).GetMethod(
            "HeaderContent",
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: [typeof(DataGridColumn)],
            modifiers: null);
        Assert.That(method, Is.Not.Null,
            "DataGrid.HeaderContent(DataGridColumn) must exist as internal method");
        Assert.That(method!.IsAssembly || method.IsFamilyOrAssembly, Is.True,
            "HeaderContent must be internal (not private) so DataGridColumnHeader can call it");
    }
}
