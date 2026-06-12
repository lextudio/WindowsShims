using System.Collections.Generic;

namespace System.Windows.Input
{
    /// <summary>
    /// Bridge subset of WPF's CommandManager. Class command bindings reuse the
    /// RoutedCommand shim's binding registry (bindings self-register at
    /// construction; registration here scopes them to the owner type), so
    /// RoutedCommand.Execute/CanExecute against a target dispatches them.
    /// Class input bindings are recorded for the future key-routing bridge but
    /// are not yet fired from input events. Requery notifications are direct
    /// (WPF batches them on the dispatcher).
    /// </summary>
    public static class CommandManager
    {
        private static readonly Dictionary<Type, List<InputBinding>> _classInputBindings = new();

        public static event EventHandler? RequerySuggested;

        public static void InvalidateRequerySuggested()
            => RequerySuggested?.Invoke(null, EventArgs.Empty);

        public static void RegisterClassCommandBinding(Type ownerType, CommandBinding commandBinding)
        {
            ArgumentNullException.ThrowIfNull(ownerType);
            ArgumentNullException.ThrowIfNull(commandBinding);

            commandBinding.SetClassOwner(ownerType);
        }

        public static void RegisterClassInputBinding(Type ownerType, InputBinding inputBinding)
        {
            ArgumentNullException.ThrowIfNull(ownerType);
            ArgumentNullException.ThrowIfNull(inputBinding);

            lock (_classInputBindings)
            {
                if (!_classInputBindings.TryGetValue(ownerType, out var bindings))
                {
                    bindings = [];
                    _classInputBindings[ownerType] = bindings;
                }

                bindings.Add(inputBinding);
            }
        }

        internal static IReadOnlyList<InputBinding> GetClassInputBindings(Type ownerType)
        {
            lock (_classInputBindings)
            {
                return _classInputBindings.TryGetValue(ownerType, out var bindings)
                    ? bindings
                    : [];
            }
        }
    }

    /// <summary>Bridge subset of WPF's InputBinding (gesture-to-command pair).</summary>
    public class InputBinding
    {
        public InputBinding(ICommand command, InputGesture gesture)
        {
            ArgumentNullException.ThrowIfNull(command);
            ArgumentNullException.ThrowIfNull(gesture);

            Command = command;
            Gesture = gesture;
        }

        public ICommand Command { get; set; }

        public InputGesture Gesture { get; set; }

        public object? CommandParameter { get; set; }

        public IInputElement? CommandTarget { get; set; }
    }
}
