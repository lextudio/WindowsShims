using System;
using System.Collections.Generic;

namespace System.Windows.Input
{
	public class CommandBindingCollection : List<CommandBinding>
	{
		public void Add(ICommand command,
			EventHandler<ExecutedRoutedEventArgs> executed) =>
			Add(new CommandBinding(command, executed));

		public void Add(ICommand command,
			EventHandler<ExecutedRoutedEventArgs> executed,
			EventHandler<CanExecuteRoutedEventArgs> canExecute) =>
			Add(new CommandBinding(command, executed, canExecute));
	}
}
