using System.Windows.Controls;
using NUnit.Framework;

namespace LeXtudio.Windows.Tests;

[TestFixture]
public sealed class DataGridEventArgsTests
{
    [Test]
    public void ColumnEventArgsStoresColumn()
    {
        var args = new DataGridColumnEventArgs(null!);

        Assert.That(args.Column, Is.Null);
    }

    [Test]
    public void SortingEventArgsTracksHandled()
    {
        var args = new DataGridSortingEventArgs(null!);

        args.Handled = true;

        Assert.That(args.Column, Is.Null);
        Assert.That(args.Handled, Is.True);
    }

    [Test]
    public void ReorderingEventArgsTracksCancelAndIndicators()
    {
        var args = new DataGridColumnReorderingEventArgs(null!);

        args.Cancel = true;

        Assert.That(args.Column, Is.Null);
        Assert.That(args.Cancel, Is.True);
        Assert.That(args.DragIndicator, Is.Null);
        Assert.That(args.DropLocationIndicator, Is.Null);
    }

    [Test]
    public void AutoGeneratingColumnEventArgsTracksColumnAndMetadata()
    {
        var replacementColumn = (DataGridColumn)null!;
        var args = new DataGridAutoGeneratingColumnEventArgs("Name", typeof(string), null!);

        args.Column = replacementColumn;
        args.Cancel = true;

        Assert.That(args.Column, Is.SameAs(replacementColumn));
        Assert.That(args.PropertyName, Is.EqualTo("Name"));
        Assert.That(args.PropertyType, Is.EqualTo(typeof(string)));
        Assert.That(args.PropertyDescriptor, Is.Null);
        Assert.That(args.Cancel, Is.True);
    }

    [Test]
    public void CellClipboardEventArgsTracksMutableContent()
    {
        var item = new object();
        var args = new DataGridCellClipboardEventArgs(item, null!, "old");

        args.Content = "new";

        Assert.That(args.Item, Is.SameAs(item));
        Assert.That(args.Column, Is.Null);
        Assert.That(args.Content, Is.EqualTo("new"));
    }
}
