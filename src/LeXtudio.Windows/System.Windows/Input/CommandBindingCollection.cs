using System;
using System.Collections.Generic;

namespace System.Windows.Input
{
	public class CommandBindingCollection : List<CommandBinding>
	{
		public void Add(ICommand command,
			ExecutedRoutedEventHandler executed) =>
			Add(new CommandBinding(command, executed));

		public void Add(ICommand command,
			ExecutedRoutedEventHandler executed,
			CanExecuteRoutedEventHandler canExecute) =>
			Add(new CommandBinding(command, executed, canExecute));
	}
}
