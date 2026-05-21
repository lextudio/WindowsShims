using System;

namespace System.Windows.Input
{
	public class CommandBinding
	{
		public ICommand Command { get; }
		private readonly ExecutedRoutedEventHandler? _executed;
		private readonly CanExecuteRoutedEventHandler? _canExecute;

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
			ExecutedRoutedEventHandler executed)
			: this(command)
		{
			_executed = executed;
		}

		public CommandBinding(
			ICommand command,
			ExecutedRoutedEventHandler executed,
			CanExecuteRoutedEventHandler canExecute)
			: this(command)
		{
			_executed = executed;
			_canExecute = canExecute;
		}

		internal void OnExecuted(object? target, ExecutedRoutedEventArgs e)
		{
			_executed?.Invoke(target ?? this, e);
		}

		internal void OnCanExecute(object? target, CanExecuteRoutedEventArgs e)
		{
			_canExecute?.Invoke(target ?? this, e);
		}
	}
}
