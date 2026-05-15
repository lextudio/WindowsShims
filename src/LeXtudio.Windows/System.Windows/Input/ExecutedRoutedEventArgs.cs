namespace System.Windows.Input
{
	public class ExecutedRoutedEventArgs : System.Windows.RoutedEventArgs
	{
		public ICommand Command { get; }
		public object   Parameter { get; }
		public bool Handled { get; set; }

		public ExecutedRoutedEventArgs(ICommand command, object parameter)
		{
			Command   = command;
			Parameter = parameter;
		}
	}
}
