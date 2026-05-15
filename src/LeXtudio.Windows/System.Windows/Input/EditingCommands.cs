namespace System.Windows.Input
{
	public static class EditingCommands
	{
		public static RoutedCommand Backspace             { get; } = new RoutedCommand("Backspace",             typeof(EditingCommands));
		public static RoutedCommand Delete                { get; } = new RoutedCommand("Delete",                typeof(EditingCommands));
		public static RoutedCommand DeleteNextWord        { get; } = new RoutedCommand("DeleteNextWord",        typeof(EditingCommands));
		public static RoutedCommand DeletePreviousWord    { get; } = new RoutedCommand("DeletePreviousWord",    typeof(EditingCommands));
		public static RoutedCommand EnterParagraphBreak   { get; } = new RoutedCommand("EnterParagraphBreak",   typeof(EditingCommands));
		public static RoutedCommand EnterLineBreak        { get; } = new RoutedCommand("EnterLineBreak",        typeof(EditingCommands));
		public static RoutedCommand TabForward            { get; } = new RoutedCommand("TabForward",            typeof(EditingCommands));
		public static RoutedCommand TabBackward           { get; } = new RoutedCommand("TabBackward",           typeof(EditingCommands));
		public static RoutedCommand MoveToLineStart       { get; } = new RoutedCommand("MoveToLineStart",       typeof(EditingCommands));
		public static RoutedCommand MoveToLineEnd         { get; } = new RoutedCommand("MoveToLineEnd",         typeof(EditingCommands));
		public static RoutedCommand SelectToLineStart     { get; } = new RoutedCommand("SelectToLineStart",     typeof(EditingCommands));
		public static RoutedCommand SelectToLineEnd       { get; } = new RoutedCommand("SelectToLineEnd",       typeof(EditingCommands));
		public static RoutedCommand MoveToDocumentStart   { get; } = new RoutedCommand("MoveToDocumentStart",   typeof(EditingCommands));
		public static RoutedCommand MoveToDocumentEnd     { get; } = new RoutedCommand("MoveToDocumentEnd",     typeof(EditingCommands));
		public static RoutedCommand SelectToDocumentStart { get; } = new RoutedCommand("SelectToDocumentStart", typeof(EditingCommands));
		public static RoutedCommand SelectToDocumentEnd   { get; } = new RoutedCommand("SelectToDocumentEnd",   typeof(EditingCommands));
		public static RoutedCommand MoveLeftByWord        { get; } = new RoutedCommand("MoveLeftByWord",        typeof(EditingCommands));
		public static RoutedCommand MoveRightByWord       { get; } = new RoutedCommand("MoveRightByWord",       typeof(EditingCommands));
		public static RoutedCommand SelectLeftByWord      { get; } = new RoutedCommand("SelectLeftByWord",      typeof(EditingCommands));
		public static RoutedCommand SelectRightByWord     { get; } = new RoutedCommand("SelectRightByWord",     typeof(EditingCommands));
	}
}
