using System.ComponentModel;
using System.Globalization;
using Xunit;
using System.Windows.Controls;

namespace LeXtudio.Windows.Tests;

public sealed class DataGridLengthTests
{
    [Fact]
    public void PixelLengthStoresAbsoluteValue()
    {
        var length = new DataGridLength(42.5);

        Assert.True(length.IsAbsolute);
        Assert.Equal(DataGridLengthUnitType.Pixel, length.UnitType);
        Assert.Equal(42.5, length.Value);
        Assert.Equal(42.5, length.DesiredValue);
        Assert.Equal(42.5, length.DisplayValue);
    }

    [Fact]
    public void StarLengthRoundTripsThroughConverter()
    {
        var converter = TypeDescriptor.GetConverter(typeof(DataGridLength));
        var length = (DataGridLength)converter.ConvertFrom(null, CultureInfo.InvariantCulture, "2*")!;

        Assert.True(length.IsStar);
        Assert.Equal(2.0, length.Value);
        Assert.Equal("2*", converter.ConvertTo(null, CultureInfo.InvariantCulture, length, typeof(string)));
    }

    [Theory]
    [InlineData("Auto", DataGridLengthUnitType.Auto)]
    [InlineData("SizeToCells", DataGridLengthUnitType.SizeToCells)]
    [InlineData("SizeToHeader", DataGridLengthUnitType.SizeToHeader)]
    public void DescriptiveLengthsParseCaseInsensitively(string text, DataGridLengthUnitType expectedUnit)
    {
        var converter = TypeDescriptor.GetConverter(typeof(DataGridLength));
        var length = (DataGridLength)converter.ConvertFromInvariantString(text.ToLowerInvariant())!;

        Assert.Equal(expectedUnit, length.UnitType);
        Assert.Equal(expectedUnit.ToString(), converter.ConvertToInvariantString(length));
    }

    [Theory]
    [InlineData("1in", 96.0)]
    [InlineData("2.54cm", 96.0)]
    [InlineData("72pt", 96.0)]
    public void PhysicalPixelUnitsConvertToDeviceIndependentPixels(string text, double expected)
    {
        var converter = TypeDescriptor.GetConverter(typeof(DataGridLength));
        var length = (DataGridLength)converter.ConvertFrom(null, CultureInfo.InvariantCulture, text)!;

        Assert.Equal(DataGridLengthUnitType.Pixel, length.UnitType);
        Assert.Equal(expected, length.Value, 0.000001);
    }

    [Fact]
    public void InvalidLengthThrows()
    {
        Assert.Throws<ArgumentException>(() => _ = new DataGridLength(double.PositiveInfinity));
    }
}
