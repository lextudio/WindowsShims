using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using NUnit.Framework;

namespace LeXtudio.Windows.Tests;

[TestFixture]
public sealed class DataGridSpineLeafTests
{
    [Test]
    public void DragEventArgsCarryOffsetsAndThumbEvents()
    {
        var started = new DragStartedEventArgs(1, 2);
        var delta = new DragDeltaEventArgs(3, 4);
        var completed = new DragCompletedEventArgs(5, 6, canceled: true);

        Assert.That(started.HorizontalOffset, Is.EqualTo(1));
        Assert.That(started.VerticalOffset, Is.EqualTo(2));
        Assert.That(started.RoutedEvent, Is.SameAs(Thumb.DragStartedEvent));

        Assert.That(delta.HorizontalChange, Is.EqualTo(3));
        Assert.That(delta.VerticalChange, Is.EqualTo(4));
        Assert.That(delta.RoutedEvent, Is.SameAs(Thumb.DragDeltaEvent));

        Assert.That(completed.HorizontalChange, Is.EqualTo(5));
        Assert.That(completed.VerticalChange, Is.EqualTo(6));
        Assert.That(completed.Canceled, Is.True);
        Assert.That(completed.RoutedEvent, Is.SameAs(Thumb.DragCompletedEvent));
    }

    [Test]
    public void ComponentResourceKeyTracksTypeAndId()
    {
        var key = new ComponentResourceKey(typeof(string), "resource");
        var same = new ComponentResourceKey(typeof(string), "resource");
        var other = new ComponentResourceKey(typeof(string), "different");

        Assert.That(key.TypeInTargetAssembly, Is.EqualTo(typeof(string)));
        Assert.That(key.ResourceId, Is.EqualTo("resource"));
        Assert.That(key, Is.EqualTo(same));
        Assert.That(key.GetHashCode(), Is.EqualTo(same.GetHashCode()));
        Assert.That(key, Is.Not.EqualTo(other));
    }

    [Test]
    public void ContainerTrackingBridgeStoresContainer()
    {
        var trackingType = typeof(System.Windows.Controls.DataGrid).Assembly
            .GetType("System.Windows.Controls.ContainerTracking`1");

        Assert.That(trackingType, Is.Not.Null);

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

        Assert.That(stored, Is.SameAs(container));
    }

    [Test]
    public void FocusNavigationDirectionMirrorsWpfOrder()
    {
        Assert.That((int)FocusNavigationDirection.Next, Is.EqualTo(0));
        Assert.That((int)FocusNavigationDirection.Previous, Is.EqualTo(1));
        Assert.That((int)FocusNavigationDirection.First, Is.EqualTo(2));
        Assert.That((int)FocusNavigationDirection.Last, Is.EqualTo(3));
        Assert.That((int)FocusNavigationDirection.Down, Is.EqualTo(7));
    }

    [Test]
    public void UncommonFieldBridgeValidatesInstance()
    {
        var field = new MS.Internal.UncommonField<string>("fallback");

        Assert.Throws<ArgumentNullException>(() => field.SetValue(null!, "x"));
        Assert.Throws<ArgumentNullException>(() => _ = field.GetValue(null!));
        Assert.Throws<ArgumentNullException>(() => field.ClearValue(null!));
    }
}
