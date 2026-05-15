using System.ComponentModel;
using System.Globalization;

namespace System.Windows;

public enum FigureUnitType
{
    Auto = 0,
    Pixel,
    Column,
    Content,
    Page,
}

[TypeConverter(typeof(FigureLengthConverter))]
public readonly struct FigureLength : IEquatable<FigureLength>
{
    private readonly double _unitValue;
    private readonly FigureUnitType _unitType;

    public FigureLength(double pixels)
        : this(pixels, FigureUnitType.Pixel)
    {
    }

    public FigureLength(double value, FigureUnitType type)
    {
        if (double.IsNaN(value))
        {
            throw new ArgumentException("Value cannot be NaN.", nameof(value));
        }

        if (double.IsInfinity(value))
        {
            throw new ArgumentException("Value cannot be infinite.", nameof(value));
        }

        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        if (type is < FigureUnitType.Auto or > FigureUnitType.Page)
        {
            throw new ArgumentException("Unknown figure unit type.", nameof(type));
        }

        if (type is FigureUnitType.Content or FigureUnitType.Page && value > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        _unitValue = type == FigureUnitType.Auto ? 0 : value;
        _unitType = type;
    }

    public bool IsAbsolute => _unitType == FigureUnitType.Pixel;

    public bool IsAuto => _unitType == FigureUnitType.Auto;

    public bool IsColumn => _unitType == FigureUnitType.Column;

    public bool IsContent => _unitType == FigureUnitType.Content;

    public bool IsPage => _unitType == FigureUnitType.Page;

    public double Value => _unitType == FigureUnitType.Auto ? 1.0 : _unitValue;

    public FigureUnitType FigureUnitType => _unitType;

    public static bool operator ==(FigureLength left, FigureLength right) =>
        left._unitType == right._unitType && left.Value.Equals(right.Value);

    public static bool operator !=(FigureLength left, FigureLength right) => !(left == right);

    public bool Equals(FigureLength other) => this == other;

    public override bool Equals(object? obj) => obj is FigureLength other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Value, _unitType);

    public override string ToString() => FigureLengthConverter.ToString(this, CultureInfo.InvariantCulture);
}

