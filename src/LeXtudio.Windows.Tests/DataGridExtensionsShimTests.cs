using DataGridExtensions;
using NUnit.Framework;

namespace LeXtudio.Windows.Tests;

[TestFixture]
public sealed class DataGridExtensionsShimTests
{
    [Test]
    public void RegexContentFilterFactoryFiltersText()
    {
        var filter = new RegexContentFilterFactory().Create("^abc\\d+$");

        Assert.That(filter.IsMatch("abc123"), Is.True);
        Assert.That(filter.IsMatch("ABC9"), Is.True);
        Assert.That(filter.IsMatch("abc"), Is.False);
    }

    [Test]
    public void HexContentFilterMatchesFormattedValue()
    {
        var filter = new HexContentFilter("00ff");

        Assert.That(filter.IsMatch(0x0000FF10), Is.True);
        Assert.That(filter.IsMatch(0x0000EF10), Is.False);
    }

    [Test]
    public void MaskContentFilterMatchesAnySelectedFlag()
    {
        var readOrWrite = new MaskContentFilter(0x0003);

        Assert.That(readOrWrite.IsMatch(0x0001), Is.True);
        Assert.That(readOrWrite.IsMatch(0x0002), Is.True);
        Assert.That(readOrWrite.IsMatch(0x0004), Is.False);
    }

    [Test]
    public void SubstringContentFilterMatchesTextCaseInsensitively()
    {
        var filter = new SubstringContentFilter("system");

        Assert.That(filter.IsMatch("System.String"), Is.True);
        Assert.That(filter.IsMatch("Microsoft.CSharp"), Is.False);
    }
}
