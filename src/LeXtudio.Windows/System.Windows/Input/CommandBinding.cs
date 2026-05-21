using System;

namespace System.Windows.Input
{
	public class CommandBinding
	{
		public ICommand Command { get; }
		internal Type? TargetType { get; }
		private readonly ExecutedRoutedEventHandler? _executed;
		private readonly CanExecuteRoutedEventHandler? _canExecute;

		public CommandBinding(ICommand command)
			: this(command, targetType: null)
		{
		}

		internal CommandBinding(ICommand command, Type? targetType)
		{
			Command = command;
			TargetType = targetType;
			if (command is RoutedCommand routedCommand)
			{
				routedCommand.RegisterBinding(this);
			}
		}

		public CommandBinding(
			ICommand command,
			ExecutedRoutedEventHandler executed)
			: this(command, targetType: null)
		{
			_executed = executed;
		}

		internal CommandBinding(
			ICommand command,
			Type? targetType,
			ExecutedRoutedEventHandler executed)
			: this(command, targetType)
		{
			_executed = executed;
		}

		public CommandBinding(
			ICommand command,
			ExecutedRoutedEventHandler executed,
			CanExecuteRoutedEventHandler canExecute)
			: this(command, targetType: null)
		{
			_executed = executed;
			_canExecute = canExecute;
		}

		internal CommandBinding(
			ICommand command,
			Type? targetType,
			ExecutedRoutedEventHandler executed,
			CanExecuteRoutedEventHandler canExecute)
			: this(command, targetType)
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

		internal bool AppliesTo(object? target)
		{
			if (TargetType == null || target == null)
			{
				return true;
			}

			return TargetType.IsInstanceOfType(target);
		}
	}
}
