using System.Windows.Input;
using Xunit;

namespace LeXtudio.Windows.Tests;

public sealed class CommandManagerBridgeTests
{
    private sealed class OwnerControl;

    private sealed class OtherControl;

    [Fact]
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

        Assert.Equal(1, executedCount);
    }

    [Fact]
    public void ClassCommandBindingDispatchesCanExecuteByTarget()
    {
        var command = new RoutedCommand("CanTest", typeof(OwnerControl));
        var binding = new CommandBinding(
            command,
            (sender, e) => { },
            (sender, e) => { e.CanExecute = false; e.Handled = true; });

        CommandManager.RegisterClassCommandBinding(typeof(OwnerControl), binding);

        Assert.False(command.CanExecute(null, new OwnerControl()));
    }

    [Fact]
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

        Assert.Equal(1, raised);
    }

    [Fact]
    public void ClassInputBindingsAreRecordedPerType()
    {
        var command = new RoutedCommand("KeyTest", typeof(OwnerControl));
        var gesture = new KeyGesture(Key.F2);

        CommandManager.RegisterClassInputBinding(typeof(OwnerControl), new InputBinding(command, gesture));
        var bindings = CommandManager.GetClassInputBindings(typeof(OwnerControl));

        Assert.True(bindings.Count >= 1);
        Assert.Same(command, bindings[^1].Command);
        Assert.Same(gesture, bindings[^1].Gesture);
        Assert.Empty(CommandManager.GetClassInputBindings(typeof(OtherControl)));
    }

    [Fact]
    public void InputBindingValidatesArguments()
    {
        var command = new RoutedCommand("X", typeof(OwnerControl));

        Assert.Throws<ArgumentNullException>(() => _ = new InputBinding(null!, new KeyGesture(Key.F2)));
        Assert.Throws<ArgumentNullException>(() => _ = new InputBinding(command, null!));
    }
}
