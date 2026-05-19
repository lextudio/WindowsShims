namespace System.Windows.Input
{
    public delegate void TextCompositionEventHandler(object sender, TextCompositionEventArgs e);

    public class TextComposition
    {
        public object Owner { get; set; }
        public string Text { get; set; } = string.Empty;
    }

    public class FrameworkTextComposition : TextComposition
    {
    }

    public static class TextCompositionManager
    {
        public static readonly System.Windows.RoutedEvent TextInputEvent = new();
        public static readonly System.Windows.RoutedEvent TextInputStartEvent = new();
        public static readonly System.Windows.RoutedEvent TextInputUpdateEvent = new();
    }

	/// <summary>
	/// Portable shim for System.Windows.Input.TextCompositionEventArgs.
	/// Carries the text that was entered by the user via IME or direct keyboard input.
	/// </summary>
	public class TextCompositionEventArgs : System.Windows.RoutedEventArgs
	{
		readonly string _systemText;

		public TextCompositionEventArgs(string text)
			: this(text, string.Empty)
		{
		}

		public TextCompositionEventArgs(string text, string systemText)
		{
			Text = text ?? string.Empty;
			_systemText = systemText ?? string.Empty;
		}

		/// <summary>Gets the composed text that was entered.</summary>
		public string Text { get; }

		/// <summary>Gets system text (e.g. from ALT key combinations).</summary>
		public string SystemText => _systemText;

		/// <summary>Gets control text when the composed input is a control character.</summary>
		public string ControlText => Text.Length > 0 && char.IsControl(Text, 0) ? Text : string.Empty;

        public TextComposition TextComposition { get; set; }
        public object Device { get; set; }
	}
}
