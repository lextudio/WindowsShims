using System.Windows.Controls;
using Xunit;

namespace LeXtudio.Windows.Tests;

public sealed class DataGridEventArgsTests
{
    [Fact]
    public void ColumnEventArgsStoresColumn()
    {
        var args = new DataGridColumnEventArgs(null!);

        Assert.Null(args.Column);
    }

    [Fact]
    public void SortingEventArgsTracksHandled()
    {
        var args = new DataGridSortingEventArgs(null!);

        args.Handled = true;

        Assert.Null(args.Column);
        Assert.True(args.Handled);
    }

    [Fact]
    public void ReorderingEventArgsTracksCancelAndIndicators()
    {
        var args = new DataGridColumnReorderingEventArgs(null!);

        args.Cancel = true;

        Assert.Null(args.Column);
        Assert.True(args.Cancel);
        Assert.Null(args.DragIndicator);
        Assert.Null(args.DropLocationIndicator);
    }

    [Fact]
    public void AutoGeneratingColumnEventArgsTracksColumnAndMetadata()
    {
        var replacementColumn = (DataGridColumn)null!;
        var args = new DataGridAutoGeneratingColumnEventArgs("Name", typeof(string), null!);

        args.Column = replacementColumn;
        args.Cancel = true;

        Assert.Same(replacementColumn, args.Column);
        Assert.Equal("Name", args.PropertyName);
        Assert.Equal(typeof(string), args.PropertyType);
        Assert.Null(args.PropertyDescriptor);
        Assert.True(args.Cancel);
    }

    [Fact]
    public void CellClipboardEventArgsTracksMutableContent()
    {
        var item = new object();
        var args = new DataGridCellClipboardEventArgs(item, null!, "old");

        args.Content = "new";

        Assert.Same(item, args.Item);
        Assert.Null(args.Column);
        Assert.Equal("new", args.Content);
    }
}
