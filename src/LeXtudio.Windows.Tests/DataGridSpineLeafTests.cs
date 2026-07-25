using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Xunit;

namespace LeXtudio.Windows.Tests;

public sealed class DataGridSpineLeafTests
{
    [Fact]
    public void DragEventArgsCarryOffsetsAndThumbEvents()
    {
        var started = new DragStartedEventArgs(1, 2);
        var delta = new DragDeltaEventArgs(3, 4);
        var completed = new DragCompletedEventArgs(5, 6, canceled: true);

        Assert.Equal(1, started.HorizontalOffset);
        Assert.Equal(2, started.VerticalOffset);
        Assert.Same(Thumb.DragStartedEvent, started.RoutedEvent);

        Assert.Equal(3, delta.HorizontalChange);
        Assert.Equal(4, delta.VerticalChange);
        Assert.Same(Thumb.DragDeltaEvent, delta.RoutedEvent);

        Assert.Equal(5, completed.HorizontalChange);
        Assert.Equal(6, completed.VerticalChange);
        Assert.True(completed.Canceled);
        Assert.Same(Thumb.DragCompletedEvent, completed.RoutedEvent);
    }

    [Fact]
    public void ComponentResourceKeyTracksTypeAndId()
    {
        var key = new ComponentResourceKey(typeof(string), "resource");
        var same = new ComponentResourceKey(typeof(string), "resource");
        var other = new ComponentResourceKey(typeof(string), "different");

        Assert.Equal(typeof(string), key.TypeInTargetAssembly);
        Assert.Equal("resource", key.ResourceId);
        Assert.Equal(same, key);
        Assert.Equal(same.GetHashCode(), key.GetHashCode());
        Assert.NotEqual(other, key);
    }

    [Fact]
    public void ContainerTrackingBridgeStoresContainer()
    {
        var trackingType = typeof(System.Windows.Controls.DataGrid).Assembly
            .GetType("System.Windows.Controls.ContainerTracking`1");

        Assert.NotNull(trackingType);

        var constructed = trackingType!.MakeGenericType(typeof(object));
        var container = new object();
        var node = Activator.CreateInstance(
            constructed,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            binder: null,
            args: [container],
            culture: null);

        var stored = constructed.GetProperty(
            "Container",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(node);

        Assert.Same(container, stored);
    }

    [Fact]
    public void FocusNavigationDirectionMirrorsWpfOrder()
    {
        Assert.Equal(0, (int)FocusNavigationDirection.Next);
        Assert.Equal(1, (int)FocusNavigationDirection.Previous);
        Assert.Equal(2, (int)FocusNavigationDirection.First);
        Assert.Equal(3, (int)FocusNavigationDirection.Last);
        Assert.Equal(7, (int)FocusNavigationDirection.Down);
    }

    [Fact]
    public void UncommonFieldBridgeValidatesInstance()
    {
        var field = new MS.Internal.UncommonField<string>("fallback");

        Assert.Throws<ArgumentNullException>(() => field.SetValue(null!, "x"));
        Assert.Throws<ArgumentNullException>(() => _ = field.GetValue(null!));
        Assert.Throws<ArgumentNullException>(() => field.ClearValue(null!));
    }
}
