namespace System.Windows.Input
{
	public static class ApplicationCommands
	{
		public static RoutedCommand Copy      { get; } = new RoutedCommand("Copy",      typeof(ApplicationCommands));
		public static RoutedCommand Cut       { get; } = new RoutedCommand("Cut",       typeof(ApplicationCommands));
		public static RoutedCommand Paste     { get; } = new RoutedCommand("Paste",     typeof(ApplicationCommands));
		public static RoutedCommand Undo      { get; } = new RoutedCommand("Undo",      typeof(ApplicationCommands));
		public static RoutedCommand Redo      { get; } = new RoutedCommand("Redo",      typeof(ApplicationCommands));
		public static RoutedCommand SelectAll { get; } = new RoutedCommand("SelectAll", typeof(ApplicationCommands));
		public static RoutedCommand Delete    { get; } = new RoutedCommand("Delete",    typeof(ApplicationCommands));
		public static RoutedCommand Find      { get; } = new RoutedCommand("Find",      typeof(ApplicationCommands));
	}
}
