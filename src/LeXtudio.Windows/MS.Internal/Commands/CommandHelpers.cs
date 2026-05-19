namespace MS.Internal.Commands
{
    public static class CommandHelpers
    {
        public static bool CanExecuteCommandSource(System.Windows.Controls.ICommandSource commandSource) => true;
        public static void ExecuteCommandSource(System.Windows.Controls.ICommandSource commandSource) { }

        // Upstream TextEditor calls these to register class-level command
        // bindings during static init. We don't have a real command routing
        // pipeline in the shim — these all no-op so the registration code
        // compiles and runs without effect.

        public static void RegisterCommandHandler(
            System.Type controlType,
            System.Windows.Input.RoutedCommand command,
            System.Windows.Input.ExecutedRoutedEventHandler executedRoutedEventHandler) { }

        public static void RegisterCommandHandler(
            System.Type controlType,
            System.Windows.Input.RoutedCommand command,
            System.Windows.Input.ExecutedRoutedEventHandler executedRoutedEventHandler,
            System.Windows.Input.CanExecuteRoutedEventHandler canExecuteRoutedEventHandler) { }

        public static void RegisterCommandHandler(
            System.Type controlType,
            System.Windows.Input.RoutedCommand command,
            System.Windows.Input.ExecutedRoutedEventHandler executedRoutedEventHandler,
            System.Windows.Input.KeyGesture key) { }

        public static void RegisterCommandHandler(
            System.Type controlType,
            System.Windows.Input.RoutedCommand command,
            System.Windows.Input.ExecutedRoutedEventHandler executedRoutedEventHandler,
            System.Windows.Input.CanExecuteRoutedEventHandler canExecuteRoutedEventHandler,
            System.Windows.Input.KeyGesture key) { }

        public static void RegisterCommandHandler(
            System.Type controlType,
            System.Windows.Input.RoutedCommand command,
            System.Windows.Input.ExecutedRoutedEventHandler executedRoutedEventHandler,
            System.Windows.Input.KeyGesture key1,
            System.Windows.Input.KeyGesture key2) { }

        public static void RegisterCommandHandler(
            System.Type controlType,
            System.Windows.Input.RoutedCommand command,
            System.Windows.Input.ExecutedRoutedEventHandler executedRoutedEventHandler,
            System.Windows.Input.CanExecuteRoutedEventHandler canExecuteRoutedEventHandler,
            System.Windows.Input.KeyGesture key1,
            System.Windows.Input.KeyGesture key2) { }

        public static void RegisterCommandHandler(
            System.Type controlType,
            System.Windows.Input.RoutedCommand command,
            System.Windows.Input.ExecutedRoutedEventHandler executedRoutedEventHandler,
            System.Windows.Input.CanExecuteRoutedEventHandler canExecuteRoutedEventHandler,
            params System.Windows.Input.InputGesture[] inputGestures) { }

        public static void RegisterCommandHandler(
            System.Type controlType,
            System.Windows.Input.RoutedCommand command,
            System.Windows.Input.ExecutedRoutedEventHandler executedRoutedEventHandler,
            string keyGestureToken,
            string keyDisplayString) { }

        public static void RegisterCommandHandler(
            System.Type controlType,
            System.Windows.Input.RoutedCommand command,
            System.Windows.Input.ExecutedRoutedEventHandler executedRoutedEventHandler,
            System.Windows.Input.CanExecuteRoutedEventHandler canExecuteRoutedEventHandler,
            string keyGestureToken,
            string keyDisplayString) { }
    }
}
