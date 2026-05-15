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
	}
}
