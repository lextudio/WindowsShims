using System.Windows.Automation.Peers;
using System.Windows.Input;
using NUnit.Framework;

namespace LeXtudio.Windows.Tests;

[TestFixture]
public sealed class FocusAndAutomationBridgeTests
{
    [Test]
    public void TraversalRequestIsLinkedAndValidates()
    {
        var request = new TraversalRequest(FocusNavigationDirection.Down);

        Assert.That(request.FocusNavigationDirection, Is.EqualTo(FocusNavigationDirection.Down));
        Assert.That(request.Wrapped, Is.False);
        Assert.Throws<System.ComponentModel.InvalidEnumArgumentException>(
            () => _ = new TraversalRequest((FocusNavigationDirection)99));
    }

    [Test]
    public void KeyboardNavigationModeMirrorsWpfOrder()
    {
        Assert.That((int)KeyboardNavigationMode.Continue, Is.EqualTo(0));
        Assert.That((int)KeyboardNavigationMode.Once, Is.EqualTo(1));
        Assert.That((int)KeyboardNavigationMode.Contained, Is.EqualTo(4));
        Assert.That((int)KeyboardNavigationMode.Local, Is.EqualTo(5));
    }

    [Test]
    public void KeyboardFocusReportsElementBack()
    {
        Assert.That(Keyboard.Focus(null), Is.Null);
    }

    [Test]
    public void AutomationStubsKeepPathsUnreachable()
    {
        Assert.That(AutomationPeer.ListenerExists(AutomationEvents.SelectionItemPatternOnElementSelected), Is.False);
        Assert.That(UIElementAutomationPeer.FromElement(null!), Is.Null);
        Assert.That(typeof(DataGridAutomationPeer).IsSubclassOf(typeof(AutomationPeer)), Is.True);
    }

    [Test]
    public void AutomationEventsMirrorWpfOrder()
    {
        Assert.That((int)AutomationEvents.InvokePatternOnInvoked, Is.EqualTo(5));
        Assert.That((int)AutomationEvents.SelectionItemPatternOnElementSelected, Is.EqualTo(8));
        Assert.That((int)AutomationEvents.PropertyChanged, Is.EqualTo(13));
    }
}
