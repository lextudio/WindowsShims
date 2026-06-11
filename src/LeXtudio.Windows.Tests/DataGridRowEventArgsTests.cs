using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using NUnit.Framework;
using WpfControl = System.Windows.Controls.Control;
using WpfRoutedEventArgs = System.Windows.RoutedEventArgs;

namespace LeXtudio.Windows.Tests;

[TestFixture]
public sealed class DataGridRowEventArgsTests
{
    [Test]
    public void RowShellTypeIsAvailable()
    {
        Assert.That(typeof(DataGridRow).IsSubclassOf(typeof(WpfControl)), Is.True);
        Assert.That(typeof(DataGridRow).GetProperty(nameof(DataGridRow.Item)), Is.Not.Null);
        Assert.That(typeof(DataGridRow).GetProperty(nameof(DataGridRow.IsEditing)), Is.Not.Null);
    }

    [Test]
    public void RowEventArgsStoresRow()
    {
        var args = new DataGridRowEventArgs(null!);

        Assert.That(args.Row, Is.Null);
    }

    [Test]
    public void BeginningEditEventArgsTracksCancel()
    {
        var editingEventArgs = new WpfRoutedEventArgs();
        var args = new DataGridBeginningEditEventArgs(null!, null!, editingEventArgs);

        args.Cancel = true;

        Assert.That(args.Column, Is.Null);
        Assert.That(args.Row, Is.Null);
        Assert.That(args.EditingEventArgs, Is.SameAs(editingEventArgs));
        Assert.That(args.Cancel, Is.True);
    }

    [Test]
    public void CellEditEndingEventArgsTracksActionAndCancel()
    {
        var args = new DataGridCellEditEndingEventArgs(null!, null!, null!, DataGridEditAction.Commit);

        args.Cancel = true;

        Assert.That(args.EditAction, Is.EqualTo(DataGridEditAction.Commit));
        Assert.That(args.EditingElement, Is.Null);
        Assert.That(args.Cancel, Is.True);
    }

    [Test]
    public void PreparingCellForEditEventArgsExposesEditingContext()
    {
        var editingEventArgs = new WpfRoutedEventArgs();
        var args = new DataGridPreparingCellForEditEventArgs(null!, null!, editingEventArgs, null!);

        Assert.That(args.Column, Is.Null);
        Assert.That(args.Row, Is.Null);
        Assert.That(args.EditingEventArgs, Is.SameAs(editingEventArgs));
        Assert.That(args.EditingElement, Is.Null);
    }

    [Test]
    public void RowDetailsEventArgsExposesRowAndDetails()
    {
        var args = new DataGridRowDetailsEventArgs(null!, null!);

        Assert.That(args.Row, Is.Null);
        Assert.That(args.DetailsElement, Is.Null);
    }

    [Test]
    public void RowEditEndingEventArgsTracksActionAndCancel()
    {
        var args = new DataGridRowEditEndingEventArgs(null!, DataGridEditAction.Cancel);

        args.Cancel = true;

        Assert.That(args.Row, Is.Null);
        Assert.That(args.EditAction, Is.EqualTo(DataGridEditAction.Cancel));
        Assert.That(args.Cancel, Is.True);
    }

    [Test]
    public void RowClipboardEventArgsFormatsCsvThroughClipboardHelper()
    {
        var args = new DataGridRowClipboardEventArgs(new object(), 0, 1, isColumnHeadersRow: false);
        args.ClipboardRowContent.Add(new DataGridClipboardCellContent(null!, null!, "plain"));
        args.ClipboardRowContent.Add(new DataGridClipboardCellContent(null!, null!, "needs,escape"));

        var csv = args.FormatClipboardCellValues(DataFormats.CommaSeparatedValue);
        var text = args.FormatClipboardCellValues(DataFormats.UnicodeText);

        Assert.That(csv, Is.EqualTo("plain,\"needs,escape\"\r\n"));
        Assert.That(text, Is.EqualTo("plain\tneeds,escape\r\n"));
    }

    [Test]
    public void SortingEventHandlerDelegateIsAvailable()
    {
        var invoke = typeof(DataGridSortingEventHandler).GetMethod("Invoke");

        Assert.That(invoke, Is.Not.Null);
        Assert.That(invoke!.GetParameters()[1].ParameterType, Is.EqualTo(typeof(DataGridSortingEventArgs)));
    }

    [Test]
    public void ItemPropertyInfoFeedsAutoGenerationArgs()
    {
        var info = new ItemPropertyInfo("Name", typeof(string), null!);
        var args = new DataGridAutoGeneratingColumnEventArgs(null!, info);

        Assert.That(args.PropertyName, Is.EqualTo("Name"));
        Assert.That(args.PropertyType, Is.EqualTo(typeof(string)));
        Assert.That(args.PropertyDescriptor, Is.Null);
    }
}
