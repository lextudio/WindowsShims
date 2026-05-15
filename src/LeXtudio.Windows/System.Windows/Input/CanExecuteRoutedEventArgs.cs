namespace System.Windows.Input
{
	public class CanExecuteRoutedEventArgs : System.Windows.RoutedEventArgs
	{
		public ICommand Command       { get; }
		public object   Parameter     { get; }
		public bool     CanExecute    { get; set; } = true;
		public bool Handled       { get; set; }
		public bool     ContinueRouting { get; set; }

		public CanExecuteRoutedEventArgs(ICommand command, object parameter)
		{
			Command   = command;
			Parameter = parameter;
		}
	}
}
