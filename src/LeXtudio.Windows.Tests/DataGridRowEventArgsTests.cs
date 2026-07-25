using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Xunit;
using WpfControl = System.Windows.Controls.Control;
using WpfRoutedEventArgs = System.Windows.RoutedEventArgs;

namespace LeXtudio.Windows.Tests;

public sealed class DataGridRowEventArgsTests
{
    [Fact]
    public void RowShellTypeIsAvailable()
    {
        Assert.True(typeof(DataGridRow).IsSubclassOf(typeof(WpfControl)));
        Assert.NotNull(typeof(DataGridRow).GetProperty(nameof(DataGridRow.Item)));
        Assert.NotNull(typeof(DataGridRow).GetProperty(nameof(DataGridRow.IsEditing)));
    }

    [Fact]
    public void RowEventArgsStoresRow()
    {
        var args = new DataGridRowEventArgs(null!);

        Assert.Null(args.Row);
    }

    [Fact]
    public void BeginningEditEventArgsTracksCancel()
    {
        var editingEventArgs = new WpfRoutedEventArgs();
        var args = new DataGridBeginningEditEventArgs(null!, null!, editingEventArgs);

        args.Cancel = true;

        Assert.Null(args.Column);
        Assert.Null(args.Row);
        Assert.Same(editingEventArgs, args.EditingEventArgs);
        Assert.True(args.Cancel);
    }

    [Fact]
    public void CellEditEndingEventArgsTracksActionAndCancel()
    {
        var args = new DataGridCellEditEndingEventArgs(null!, null!, null!, DataGridEditAction.Commit);

        args.Cancel = true;

        Assert.Equal(DataGridEditAction.Commit, args.EditAction);
        Assert.Null(args.EditingElement);
        Assert.True(args.Cancel);
    }

    [Fact]
    public void PreparingCellForEditEventArgsExposesEditingContext()
    {
        var editingEventArgs = new WpfRoutedEventArgs();
        var args = new DataGridPreparingCellForEditEventArgs(null!, null!, editingEventArgs, null!);

        Assert.Null(args.Column);
        Assert.Null(args.Row);
        Assert.Same(editingEventArgs, args.EditingEventArgs);
        Assert.Null(args.EditingElement);
    }

    [Fact]
    public void RowDetailsEventArgsExposesRowAndDetails()
    {
        var args = new DataGridRowDetailsEventArgs(null!, null!);

        Assert.Null(args.Row);
        Assert.Null(args.DetailsElement);
    }

    [Fact]
    public void RowEditEndingEventArgsTracksActionAndCancel()
    {
        var args = new DataGridRowEditEndingEventArgs(null!, DataGridEditAction.Cancel);

        args.Cancel = true;

        Assert.Null(args.Row);
        Assert.Equal(DataGridEditAction.Cancel, args.EditAction);
        Assert.True(args.Cancel);
    }

    [Fact]
    public void RowClipboardEventArgsFormatsCsvThroughClipboardHelper()
    {
        var args = new DataGridRowClipboardEventArgs(new object(), 0, 1, isColumnHeadersRow: false);
        args.ClipboardRowContent.Add(new DataGridClipboardCellContent(null!, null!, "plain"));
        args.ClipboardRowContent.Add(new DataGridClipboardCellContent(null!, null!, "needs,escape"));

        var csv = args.FormatClipboardCellValues(DataFormats.CommaSeparatedValue);
        var text = args.FormatClipboardCellValues(DataFormats.UnicodeText);

        Assert.Equal("plain,\"needs,escape\"\r\n", csv);
        Assert.Equal("plain\tneeds,escape\r\n", text);
    }

    [Fact]
    public void SortingEventHandlerDelegateIsAvailable()
    {
        var invoke = typeof(DataGridSortingEventHandler).GetMethod("Invoke");

        Assert.NotNull(invoke);
        Assert.Equal(typeof(DataGridSortingEventArgs), invoke!.GetParameters()[1].ParameterType);
    }

    [Fact]
    public void ItemPropertyInfoFeedsAutoGenerationArgs()
    {
        var info = new ItemPropertyInfo("Name", typeof(string), null!);
        var args = new DataGridAutoGeneratingColumnEventArgs(null!, info);

        Assert.Equal("Name", args.PropertyName);
        Assert.Equal(typeof(string), args.PropertyType);
        Assert.Null(args.PropertyDescriptor);
    }
}
