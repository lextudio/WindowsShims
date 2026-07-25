using DataGridExtensions;
using Xunit;

namespace LeXtudio.Windows.Tests;

public sealed class DataGridExtensionsShimTests
{
    [Fact]
    public void RegexContentFilterFactoryFiltersText()
    {
        var filter = new RegexContentFilterFactory().Create("^abc\\d+$");

        Assert.True(filter.IsMatch("abc123"));
        Assert.True(filter.IsMatch("ABC9"));
        Assert.False(filter.IsMatch("abc"));
    }

    [Fact]
    public void HexContentFilterMatchesFormattedValue()
    {
        var filter = new HexContentFilter("00ff");

        Assert.True(filter.IsMatch(0x0000FF10));
        Assert.False(filter.IsMatch(0x0000EF10));
    }

    [Fact]
    public void MaskContentFilterMatchesAnySelectedFlag()
    {
        var readOrWrite = new MaskContentFilter(0x0003);

        Assert.True(readOrWrite.IsMatch(0x0001));
        Assert.True(readOrWrite.IsMatch(0x0002));
        Assert.False(readOrWrite.IsMatch(0x0004));
    }

    [Fact]
    public void SubstringContentFilterMatchesTextCaseInsensitively()
    {
        var filter = new SubstringContentFilter("system");

        Assert.True(filter.IsMatch("System.String"));
        Assert.False(filter.IsMatch("Microsoft.CSharp"));
    }
}
