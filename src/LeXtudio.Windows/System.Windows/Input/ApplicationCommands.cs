namespace System.Windows.Input
{
	public static class ApplicationCommands
	{
		public static RoutedUICommand Copy      { get; } = new RoutedUICommand("Copy",      "Copy",      typeof(ApplicationCommands));
		public static RoutedUICommand Cut       { get; } = new RoutedUICommand("Cut",       "Cut",       typeof(ApplicationCommands));
		public static RoutedUICommand Paste     { get; } = new RoutedUICommand("Paste",     "Paste",     typeof(ApplicationCommands));
		public static RoutedUICommand Undo      { get; } = new RoutedUICommand("Undo",      "Undo",      typeof(ApplicationCommands));
		public static RoutedUICommand Redo      { get; } = new RoutedUICommand("Redo",      "Redo",      typeof(ApplicationCommands));
		public static RoutedUICommand SelectAll { get; } = new RoutedUICommand("SelectAll", "SelectAll", typeof(ApplicationCommands));
		public static RoutedUICommand Delete    { get; } = new RoutedUICommand("Delete",    "Delete",    typeof(ApplicationCommands));
		public static RoutedUICommand Find           { get; } = new RoutedUICommand("Find",           "Find",           typeof(ApplicationCommands));
		public static RoutedUICommand CorrectionList { get; } = new RoutedUICommand("CorrectionList", "CorrectionList", typeof(ApplicationCommands));
	}
}
