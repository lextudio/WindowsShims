namespace System.Windows.Input
{
	public class KeyGesture : InputGesture
	{
		public Key            Key           { get; }
		public ModifierKeys   Modifiers     { get; }
		public string         DisplayString { get; }

		public KeyGesture(Key key)
			: this(key, ModifierKeys.None) { }

		public KeyGesture(Key key, ModifierKeys modifiers)
			: this(key, modifiers, string.Empty) { }

		public KeyGesture(Key key, ModifierKeys modifiers, string displayString)
		{
			Key           = key;
			Modifiers     = modifiers;
			DisplayString = displayString ?? string.Empty;
		}

		// WPF helper that parses "Ctrl+Z" style gesture strings. Shim returns a
		// gesture with no key; commands invoked through the shim won't dispatch
		// on real keystrokes.
		public static KeyGesture CreateFromResourceStrings(string keyGestureToken, string keyDisplayString)
			=> new KeyGesture(Key.None, ModifierKeys.None, keyDisplayString ?? string.Empty);
	}
}
