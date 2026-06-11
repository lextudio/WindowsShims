using System.ComponentModel;
using System.Globalization;
using NUnit.Framework;
using System.Windows.Controls;

namespace LeXtudio.Windows.Tests;

[TestFixture]
public sealed class DataGridLengthTests
{
    [Test]
    public void PixelLengthStoresAbsoluteValue()
    {
        var length = new DataGridLength(42.5);

        Assert.That(length.IsAbsolute, Is.True);
        Assert.That(length.UnitType, Is.EqualTo(DataGridLengthUnitType.Pixel));
        Assert.That(length.Value, Is.EqualTo(42.5));
        Assert.That(length.DesiredValue, Is.EqualTo(42.5));
        Assert.That(length.DisplayValue, Is.EqualTo(42.5));
    }

    [Test]
    public void StarLengthRoundTripsThroughConverter()
    {
        var converter = TypeDescriptor.GetConverter(typeof(DataGridLength));
        var length = (DataGridLength)converter.ConvertFrom(null, CultureInfo.InvariantCulture, "2*")!;

        Assert.That(length.IsStar, Is.True);
        Assert.That(length.Value, Is.EqualTo(2.0));
        Assert.That(converter.ConvertTo(null, CultureInfo.InvariantCulture, length, typeof(string)), Is.EqualTo("2*"));
    }

    [TestCase("Auto", DataGridLengthUnitType.Auto)]
    [TestCase("SizeToCells", DataGridLengthUnitType.SizeToCells)]
    [TestCase("SizeToHeader", DataGridLengthUnitType.SizeToHeader)]
    public void DescriptiveLengthsParseCaseInsensitively(string text, DataGridLengthUnitType expectedUnit)
    {
        var converter = TypeDescriptor.GetConverter(typeof(DataGridLength));
        var length = (DataGridLength)converter.ConvertFromInvariantString(text.ToLowerInvariant())!;

        Assert.That(length.UnitType, Is.EqualTo(expectedUnit));
        Assert.That(converter.ConvertToInvariantString(length), Is.EqualTo(expectedUnit.ToString()));
    }

    [TestCase("1in", 96.0)]
    [TestCase("2.54cm", 96.0)]
    [TestCase("72pt", 96.0)]
    public void PhysicalPixelUnitsConvertToDeviceIndependentPixels(string text, double expected)
    {
        var converter = TypeDescriptor.GetConverter(typeof(DataGridLength));
        var length = (DataGridLength)converter.ConvertFrom(null, CultureInfo.InvariantCulture, text)!;

        Assert.That(length.UnitType, Is.EqualTo(DataGridLengthUnitType.Pixel));
        Assert.That(length.Value, Is.EqualTo(expected).Within(0.000001));
    }

    [Test]
    public void InvalidLengthThrows()
    {
        Assert.Throws<ArgumentException>(() => _ = new DataGridLength(double.PositiveInfinity));
    }
}
