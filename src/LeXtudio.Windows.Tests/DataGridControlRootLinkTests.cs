using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NUnit.Framework;

namespace LeXtudio.Windows.Tests;

// Session 22: the upstream DataGrid.cs control root is linked and compiles.
// These tests pin the merge points: upstream members that only exist when the
// linked file is active, the bridge contracts the link depends on, and the
// honest stubs that keep WPF paths inert.
[TestFixture]
public sealed class DataGridControlRootLinkTests
{
    [Test]
    public void ControlRootIsLinkedUpstream()
    {
        // These members exist only in the upstream control root, not the old
        // local shell: editing commands and the frozen-column surface.
        Assert.That(typeof(DataGrid).GetField("BeginEditCommand"), Is.Not.Null);
        Assert.That(typeof(DataGrid).GetField("CommitEditCommand"), Is.Not.Null);
        Assert.That(typeof(DataGrid).GetField("CancelEditCommand"), Is.Not.Null);
        Assert.That(typeof(DataGrid).GetProperty("FrozenColumnCount"), Is.Not.Null);
        Assert.That(typeof(DataGrid).GetProperty("ClipboardCopyMode"), Is.Not.Null);
    }

    [Test]
    public void LocalPartialMergesWithUpstream()
    {
        // UpdateVisualState lives in the local partial; ChangeVisualState is
        // the upstream override. Both resolving on one type proves the
        // HAS_UNO partial fork guard merged the two parts.
        var updateVisualState = typeof(DataGrid).GetMethod(
            "UpdateVisualState", BindingFlags.Instance | BindingFlags.NonPublic, Type.EmptyTypes);
        var changeVisualState = typeof(DataGrid).GetMethod(
            "ChangeVisualState", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(updateVisualState, Is.Not.Null);
        Assert.That(changeVisualState, Is.Not.Null);
        Assert.That(changeVisualState!.DeclaringType, Is.EqualTo(typeof(DataGrid)));
    }

    [Test]
    public void DeleteAndSelectAllCommandsAreRoutedUICommands()
    {
        // Upstream returns ApplicationCommands.Delete/SelectAll as
        // RoutedUICommand; the shim's ApplicationCommands was retyped to match.
        var deleteCommand = typeof(DataGrid).GetProperty("DeleteCommand");
        var selectAllCommand = typeof(DataGrid).GetProperty("SelectAllCommand");

        Assert.That(deleteCommand, Is.Not.Null);
        Assert.That(deleteCommand!.PropertyType, Is.EqualTo(typeof(RoutedUICommand)));
        Assert.That(selectAllCommand, Is.Not.Null);
        Assert.That(selectAllCommand!.PropertyType, Is.EqualTo(typeof(RoutedUICommand)));
        Assert.That(ApplicationCommands.Delete, Is.InstanceOf<RoutedUICommand>());
    }

    [Test]
    public void FocusBorderBrushKeyIsComponentResourceKey()
    {
        // Upstream stores SystemResourceKey.DataGridFocusBorderBrushKey in a
        // ComponentResourceKey-typed property; the stub key derives from it.
        var key = typeof(DataGrid).GetProperty("FocusBorderBrushKey");

        Assert.That(key, Is.Not.Null);
        Assert.That(key!.PropertyType, Is.EqualTo(typeof(ComponentResourceKey)));
    }

    [Test]
    public void KeyEventArgsRoutesIntoBeginEdit()
    {
        // Upstream passes KeyEventArgs straight to BeginEdit(RoutedEventArgs).
        Assert.That(typeof(KeyEventArgs).IsSubclassOf(typeof(System.Windows.RoutedEventArgs)), Is.True);
    }

    [Test]
    public void VectorLengthIsEuclidean()
    {
        var v = new Vector(3, 4);

        Assert.That(v.Length, Is.EqualTo(5));
    }

    [Test]
    public void MouseCaptureReportsFailureForNonElements()
    {
        // WPF Mouse.Capture returns bool; non-UIElement targets cannot be
        // captured by the shim, and drag paths fall through honestly.
        Assert.That(Mouse.Capture(null!), Is.False);
        Assert.That(Mouse.Capture((IInputElement)null!, CaptureMode.SubTree), Is.False);
    }

    [Test]
    public void PresenterShellsExposeLinkContract()
    {
        var presenters = typeof(DataGrid).Assembly;
        var cellsPresenter = presenters.GetType("System.Windows.Controls.Primitives.DataGridCellsPresenter");
        var detailsPresenter = presenters.GetType("System.Windows.Controls.Primitives.DataGridDetailsPresenter");
        var rowHeader = presenters.GetType("System.Windows.Controls.Primitives.DataGridRowHeader");

        Assert.That(cellsPresenter, Is.Not.Null);
        Assert.That(detailsPresenter, Is.Not.Null);
        Assert.That(rowHeader, Is.Not.Null);
        Assert.That(
            cellsPresenter!.GetProperty("DataGridOwner", BindingFlags.Instance | BindingFlags.NonPublic),
            Is.Not.Null);
        Assert.That(
            detailsPresenter!.GetProperty("DetailsElement", BindingFlags.Instance | BindingFlags.NonPublic),
            Is.Not.Null);
        Assert.That(
            rowHeader!.GetProperty("ParentRow", BindingFlags.Instance | BindingFlags.NonPublic),
            Is.Not.Null);
    }

    [Test]
    public void DispatcherSupportsWpfBeginInvokeShapes()
    {
        // The control root uses both argument orders; both overloads must exist.
        var delegateFirst = typeof(System.Windows.Threading.Dispatcher).GetMethod(
            "BeginInvoke",
            [typeof(System.Windows.Threading.DispatcherOperationCallback), typeof(System.Windows.Threading.DispatcherPriority), typeof(object)]);
        var priorityFirst = typeof(System.Windows.Threading.Dispatcher).GetMethod(
            "BeginInvoke",
            [typeof(System.Windows.Threading.DispatcherPriority), typeof(Delegate), typeof(object)]);

        Assert.That(delegateFirst, Is.Not.Null);
        Assert.That(priorityFirst, Is.Not.Null);
    }
}
