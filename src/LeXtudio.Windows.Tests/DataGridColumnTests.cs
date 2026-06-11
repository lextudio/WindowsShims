using NUnit.Framework;
using System.Windows.Controls;
using System.Windows.Data;

namespace LeXtudio.Windows.Tests;

[TestFixture]
public sealed class DataGridColumnTests
{
    [Test]
    public void ColumnShellTypeIsAvailable()
    {
        Assert.That(typeof(DataGridColumn).GetProperty(nameof(DataGridColumn.Header)), Is.Not.Null);
        Assert.That(typeof(DataGridColumn).GetProperty(nameof(DataGridColumn.Width)), Is.Not.Null);
        Assert.That(typeof(DataGridColumn).GetProperty(nameof(DataGridColumn.ClipboardContentBinding)), Is.Not.Null);
    }

    [Test]
    public void ClipboardCellContentCanBeCreatedWithoutAColumn()
    {
        var item = new object();
        var content = new DataGridClipboardCellContent(item, null!, "value");

        Assert.That(content.Item, Is.SameAs(item));
        Assert.That(content.Column, Is.Null);
        Assert.That(content.Content, Is.EqualTo("value"));
    }
}
