using System;

namespace System.Windows.Input
{
	public class CommandBinding
	{
		public ICommand Command { get; }
		private readonly EventHandler<ExecutedRoutedEventArgs>? _executed;
		private readonly EventHandler<CanExecuteRoutedEventArgs>? _canExecute;

		public CommandBinding(ICommand command)
		{
			Command = command;
			if (command is RoutedCommand routedCommand)
			{
				routedCommand.RegisterBinding(this);
			}
		}

		public CommandBinding(
			ICommand command,
			EventHandler<ExecutedRoutedEventArgs> executed)
			: this(command)
		{
			_executed = executed;
		}

		public CommandBinding(
			ICommand command,
			EventHandler<ExecutedRoutedEventArgs> executed,
			EventHandler<CanExecuteRoutedEventArgs> canExecute)
			: this(command)
		{
			_executed = executed;
			_canExecute = canExecute;
		}

		internal void OnExecuted(ExecutedRoutedEventArgs e)
		{
			_executed?.Invoke(this, e);
		}

		internal void OnCanExecute(CanExecuteRoutedEventArgs e)
		{
			_canExecute?.Invoke(this, e);
		}
	}
}
