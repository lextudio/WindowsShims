using System.Windows.Input;
using NUnit.Framework;

namespace LeXtudio.Windows.Tests;

[TestFixture]
public sealed class CommandManagerBridgeTests
{
    private sealed class OwnerControl;

    private sealed class OtherControl;

    [Test]
    public void ClassCommandBindingScopesToOwnerType()
    {
        var command = new RoutedCommand("Test", typeof(OwnerControl));
        var executedCount = 0;
        var binding = new CommandBinding(
            command,
            (sender, e) => executedCount++,
            (sender, e) => { e.CanExecute = true; e.Handled = true; });

        CommandManager.RegisterClassCommandBinding(typeof(OwnerControl), binding);

        command.Execute(null, new OwnerControl());
        command.Execute(null, new OtherControl());

        Assert.That(executedCount, Is.EqualTo(1));
    }

    [Test]
    public void ClassCommandBindingDispatchesCanExecuteByTarget()
    {
        var command = new RoutedCommand("CanTest", typeof(OwnerControl));
        var binding = new CommandBinding(
            command,
            (sender, e) => { },
            (sender, e) => { e.CanExecute = false; e.Handled = true; });

        CommandManager.RegisterClassCommandBinding(typeof(OwnerControl), binding);

        Assert.That(command.CanExecute(null, new OwnerControl()), Is.False);
    }

    [Test]
    public void InvalidateRequerySuggestedRaisesEvent()
    {
        var raised = 0;
        EventHandler handler = (_, _) => raised++;
        CommandManager.RequerySuggested += handler;

        try
        {
            CommandManager.InvalidateRequerySuggested();
        }
        finally
        {
            CommandManager.RequerySuggested -= handler;
        }

        Assert.That(raised, Is.EqualTo(1));
    }

    [Test]
    public void ClassInputBindingsAreRecordedPerType()
    {
        var command = new RoutedCommand("KeyTest", typeof(OwnerControl));
        var gesture = new KeyGesture(Key.F2);

        CommandManager.RegisterClassInputBinding(typeof(OwnerControl), new InputBinding(command, gesture));
        var bindings = CommandManager.GetClassInputBindings(typeof(OwnerControl));

        Assert.That(bindings, Has.Count.GreaterThanOrEqualTo(1));
        Assert.That(bindings[^1].Command, Is.SameAs(command));
        Assert.That(bindings[^1].Gesture, Is.SameAs(gesture));
        Assert.That(CommandManager.GetClassInputBindings(typeof(OtherControl)), Is.Empty);
    }

    [Test]
    public void InputBindingValidatesArguments()
    {
        var command = new RoutedCommand("X", typeof(OwnerControl));

        Assert.Throws<ArgumentNullException>(() => _ = new InputBinding(null!, new KeyGesture(Key.F2)));
        Assert.Throws<ArgumentNullException>(() => _ = new InputBinding(command, null!));
    }
}
