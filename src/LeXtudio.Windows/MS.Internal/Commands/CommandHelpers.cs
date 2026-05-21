namespace MS.Internal.Commands
{
    public static class CommandHelpers
    {
        public static bool CanExecuteCommandSource(System.Windows.Controls.ICommandSource commandSource) => true;
        public static void ExecuteCommandSource(System.Windows.Controls.ICommandSource commandSource) { }

        // Upstream WPF registers class-level command handlers here. Uno does
        // not have full routed command infrastructure, but registering the
        // bindings against the RoutedCommand itself lets explicit command
        // execution reuse the WPF handler bodies with minimal shim code.

        public static void RegisterCommandHandler(
            System.Type controlType,
            System.Windows.Input.RoutedCommand command,
            System.Windows.Input.ExecutedRoutedEventHandler executedRoutedEventHandler)
            => new System.Windows.Input.CommandBinding(command, executedRoutedEventHandler);

        public static void RegisterCommandHandler(
            System.Type controlType,
            System.Windows.Input.RoutedCommand command,
            System.Windows.Input.ExecutedRoutedEventHandler executedRoutedEventHandler,
            System.Windows.Input.CanExecuteRoutedEventHandler canExecuteRoutedEventHandler)
            => new System.Windows.Input.CommandBinding(command, executedRoutedEventHandler, canExecuteRoutedEventHandler);

        public static void RegisterCommandHandler(
            System.Type controlType,
            System.Windows.Input.RoutedCommand command,
            System.Windows.Input.ExecutedRoutedEventHandler executedRoutedEventHandler,
            System.Windows.Input.KeyGesture key)
            => new System.Windows.Input.CommandBinding(command, executedRoutedEventHandler);

        public static void RegisterCommandHandler(
            System.Type controlType,
            System.Windows.Input.RoutedCommand command,
            System.Windows.Input.ExecutedRoutedEventHandler executedRoutedEventHandler,
            System.Windows.Input.CanExecuteRoutedEventHandler canExecuteRoutedEventHandler,
            System.Windows.Input.KeyGesture key)
            => new System.Windows.Input.CommandBinding(command, executedRoutedEventHandler, canExecuteRoutedEventHandler);

        public static void RegisterCommandHandler(
            System.Type controlType,
            System.Windows.Input.RoutedCommand command,
            System.Windows.Input.ExecutedRoutedEventHandler executedRoutedEventHandler,
            System.Windows.Input.KeyGesture key1,
            System.Windows.Input.KeyGesture key2)
            => new System.Windows.Input.CommandBinding(command, executedRoutedEventHandler);

        public static void RegisterCommandHandler(
            System.Type controlType,
            System.Windows.Input.RoutedCommand command,
            System.Windows.Input.ExecutedRoutedEventHandler executedRoutedEventHandler,
            System.Windows.Input.CanExecuteRoutedEventHandler canExecuteRoutedEventHandler,
            System.Windows.Input.KeyGesture key1,
            System.Windows.Input.KeyGesture key2)
            => new System.Windows.Input.CommandBinding(command, executedRoutedEventHandler, canExecuteRoutedEventHandler);

        public static void RegisterCommandHandler(
            System.Type controlType,
            System.Windows.Input.RoutedCommand command,
            System.Windows.Input.ExecutedRoutedEventHandler executedRoutedEventHandler,
            System.Windows.Input.CanExecuteRoutedEventHandler canExecuteRoutedEventHandler,
            params System.Windows.Input.InputGesture[] inputGestures)
            => new System.Windows.Input.CommandBinding(command, executedRoutedEventHandler, canExecuteRoutedEventHandler);

        public static void RegisterCommandHandler(
            System.Type controlType,
            System.Windows.Input.RoutedCommand command,
            System.Windows.Input.ExecutedRoutedEventHandler executedRoutedEventHandler,
            string keyGestureToken,
            string keyDisplayString)
            => new System.Windows.Input.CommandBinding(command, executedRoutedEventHandler);

        public static void RegisterCommandHandler(
            System.Type controlType,
            System.Windows.Input.RoutedCommand command,
            System.Windows.Input.ExecutedRoutedEventHandler executedRoutedEventHandler,
            System.Windows.Input.CanExecuteRoutedEventHandler canExecuteRoutedEventHandler,
            string keyGestureToken,
            string keyDisplayString)
            => new System.Windows.Input.CommandBinding(command, executedRoutedEventHandler, canExecuteRoutedEventHandler);
    }
}
