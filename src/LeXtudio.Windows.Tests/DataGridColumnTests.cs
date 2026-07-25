using Xunit;
using System.Windows.Controls;
using System.Windows.Data;

namespace LeXtudio.Windows.Tests;

public sealed class DataGridColumnTests
{
    [Fact]
    public void ColumnShellTypeIsAvailable()
    {
        Assert.NotNull(typeof(DataGridColumn).GetProperty(nameof(DataGridColumn.Header)));
        Assert.NotNull(typeof(DataGridColumn).GetProperty(nameof(DataGridColumn.Width)));
        Assert.NotNull(typeof(DataGridColumn).GetProperty(nameof(DataGridColumn.ClipboardContentBinding)));
    }

    [Fact]
    public void ClipboardCellContentCanBeCreatedWithoutAColumn()
    {
        var item = new object();
        var content = new DataGridClipboardCellContent(item, null!, "value");

        Assert.Same(item, content.Item);
        Assert.Null(content.Column);
        Assert.Equal("value", content.Content);
    }
}
