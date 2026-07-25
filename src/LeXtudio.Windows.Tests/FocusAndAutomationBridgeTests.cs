using System.Windows.Automation.Peers;
using System.Windows.Input;
using Xunit;

namespace LeXtudio.Windows.Tests;

public sealed class FocusAndAutomationBridgeTests
{
    [Fact]
    public void TraversalRequestIsLinkedAndValidates()
    {
        var request = new TraversalRequest(FocusNavigationDirection.Down);

        Assert.Equal(FocusNavigationDirection.Down, request.FocusNavigationDirection);
        Assert.False(request.Wrapped);
        Assert.Throws<System.ComponentModel.InvalidEnumArgumentException>(
            () => _ = new TraversalRequest((FocusNavigationDirection)99));
    }

    [Fact]
    public void KeyboardNavigationModeMirrorsWpfOrder()
    {
        Assert.Equal(0, (int)KeyboardNavigationMode.Continue);
        Assert.Equal(1, (int)KeyboardNavigationMode.Once);
        Assert.Equal(4, (int)KeyboardNavigationMode.Contained);
        Assert.Equal(5, (int)KeyboardNavigationMode.Local);
    }

    [Fact]
    public void KeyboardFocusReportsElementBack()
    {
        Assert.Null(Keyboard.Focus(null));
    }

    [Fact]
    public void AutomationStubsKeepPathsUnreachable()
    {
        Assert.False(AutomationPeer.ListenerExists(AutomationEvents.SelectionItemPatternOnElementSelected));
        Assert.Null(UIElementAutomationPeer.FromElement(null!));
        Assert.True(typeof(DataGridAutomationPeer).IsSubclassOf(typeof(AutomationPeer)));
    }

    [Fact]
    public void AutomationEventsMirrorWpfOrder()
    {
        Assert.Equal(5, (int)AutomationEvents.InvokePatternOnInvoked);
        Assert.Equal(8, (int)AutomationEvents.SelectionItemPatternOnElementSelected);
        Assert.Equal(13, (int)AutomationEvents.PropertyChanged);
    }
}
