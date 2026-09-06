using System.Windows.Media;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Xunit;
using Matrix = System.Windows.Media.Matrix;

namespace LeXtudio.Windows.Tests;

/// <summary>
/// The WPF value-type surface that Windows.Foundation.Rect and the WinUI transform types
/// do not carry. Shared across the port; first needed by the fixed-document family.
/// </summary>
public sealed class RectAndTransformExtensionsTests
{
    [Fact]
    public void OffsetMovesTheRectInPlace()
    {
        var rect = new Rect(10, 20, 30, 40);

        rect.Offset(5, -5);

        Assert.Equal(new Rect(15, 15, 30, 40), rect);
    }

    [Fact]
    public void OffsetLeavesAnEmptyRectEmpty()
    {
        var rect = Rect.Empty;

        rect.Offset(5, 5);

        Assert.True(rect.IsEmpty);
    }

    [Fact]
    public void InflateGrowsOnAllFourSides()
    {
        var rect = new Rect(10, 10, 20, 20);

        rect.Inflate(2, 3);

        Assert.Equal(new Rect(8, 7, 24, 26), rect);
    }

    [Fact]
    public void InflatePastZeroCollapsesToEmpty()
    {
        var rect = new Rect(0, 0, 4, 4);

        rect.Inflate(-3, -3);

        Assert.True(rect.IsEmpty);
    }

    [Fact]
    public void CornersMatchTheEdges()
    {
        var rect = new Rect(10, 20, 30, 40);

        Assert.Equal(new Point(10, 20), rect.TopLeft);
        Assert.Equal(new Point(40, 20), rect.TopRight);
        Assert.Equal(new Point(10, 60), rect.BottomLeft);
        Assert.Equal(new Point(40, 60), rect.BottomRight);
    }

    [Theory]
    [InlineData(0, 0, 10, 10, true)]      // overlapping
    [InlineData(20, 20, 10, 10, true)]    // touching at a corner
    [InlineData(21, 21, 10, 10, false)]   // clear of it
    public void IntersectsWithReportsSharedArea(double x, double y, double w, double h, bool expected)
    {
        var rect = new Rect(5, 5, 15, 15);

        Assert.Equal(expected, rect.IntersectsWith(new Rect(x, y, w, h)));
    }

    [Theory]
    [InlineData(6, 6, 5, 5, true)]        // fully inside
    [InlineData(5, 5, 15, 15, true)]      // exactly the same rect
    [InlineData(18, 18, 5, 5, false)]     // hangs off the bottom-right
    public void ContainsReportsFullEnclosure(double x, double y, double w, double h, bool expected)
    {
        var rect = new Rect(5, 5, 15, 15);

        Assert.Equal(expected, rect.Contains(new Rect(x, y, w, h)));
    }

    [Fact]
    public void EmptyRectContainsNothingAndIsContainedByNothing()
    {
        Assert.False(Rect.Empty.Contains(new Rect(0, 0, 1, 1)));
        Assert.False(new Rect(0, 0, 10, 10).Contains(Rect.Empty));
    }

    [Fact]
    public void EmptyRectIntersectsNothing()
    {
        Assert.False(Rect.Empty.IntersectsWith(new Rect(0, 0, 10, 10)));
        Assert.False(new Rect(0, 0, 10, 10).IntersectsWith(Rect.Empty));
    }

    [Fact]
    public void TransformTakesTheBoundingBoxOfTheRotatedCorners()
    {
        var rect = new Rect(0, 0, 2, 4);
        // 90-degree rotation about the origin.
        var rotate = new Matrix { M11 = 0, M12 = 1, M21 = -1, M22 = 0 };

        rect.Transform(rotate);

        Assert.Equal(new Rect(-4, 0, 4, 2), rect);
    }

    [Fact]
    public void ScaleWithANegativeFactorFlipsAndNormalizes()
    {
        var rect = new Rect(1, 1, 2, 2);

        rect.Scale(-1, 1);

        Assert.Equal(new Rect(-3, 1, 2, 2), rect);
    }

    [Fact]
    public void FromSizeProducesAnOriginRect()
    {
        Assert.Equal(new Rect(0, 0, 5, 7), RectExtensions.FromSize(new Size(5, 7)));
        Assert.True(RectExtensions.FromSize(Size.Empty).IsEmpty);
    }

    [Fact]
    public void MatrixMultiplyAppliesTheLeftOperandFirst()
    {
        var translate = new Matrix { M11 = 1, M22 = 1, OffsetX = 10, OffsetY = 0 };
        var scale = new Matrix { M11 = 2, M22 = 2 };

        // Translate then scale: (0,0) -> (10,0) -> (20,0).
        Assert.Equal(new Point(20, 0), (translate * scale).Transform(new Point(0, 0)));

        // Scale then translate: (0,0) -> (0,0) -> (10,0).
        Assert.Equal(new Point(10, 0), (scale * translate).Transform(new Point(0, 0)));
    }

    [Fact]
    public void MatrixIdentityIsRecognised()
    {
        Assert.True(Matrix.Identity.IsIdentity);
        Assert.False(new Matrix { M11 = 2, M22 = 1 }.IsIdentity);
    }

    // Transform.Value, Transform.Identity and GeneralTransform.AffineTransform are not
    // covered here: constructing a WinUI MatrixTransform needs a DispatcherQueue, which the
    // model test project has no app to provide. They belong in an integration test.
}
