using System;

namespace System.Windows.Input
{
	public class CommandBinding
	{
		public ICommand Command { get; }
		internal Type? TargetType { get; private set; }
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

		// Class registration (CommandManager.RegisterClassCommandBinding) scopes a
		// binding constructed with the public constructors to its owner type.
		internal void SetClassOwner(Type ownerType)
		{
			TargetType ??= ownerType;
		}

		internal bool AppliesTo(object? target)
		{
			if (TargetType == null || target == null)
			{
				return true;
			}

			if (TargetType.IsInstanceOfType(target))
			{
				return true;
			}

			// WPF routes class-scoped commands up the element tree: a binding
			// owned by an ancestor type (e.g. DataGrid) applies when executed
			// against a descendant target (e.g. a DataGridCell). Walk the visual
			// tree from the target looking for a TargetType ancestor.
			if (target is Microsoft.UI.Xaml.DependencyObject node)
			{
				for (var parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(node);
					 parent is not null;
					 parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(parent))
				{
					if (TargetType.IsInstanceOfType(parent))
					{
						return true;
					}
				}
			}

			return false;
		}

		internal object? ResolveInvocationTarget(object? target)
		{
			if (TargetType == null || target == null)
			{
				return target;
			}

			if (TargetType.IsInstanceOfType(target))
			{
				return target;
			}

			if (target is Microsoft.UI.Xaml.DependencyObject node)
			{
				for (var parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(node);
					 parent is not null;
					 parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(parent))
				{
					if (TargetType.IsInstanceOfType(parent))
					{
						return parent;
					}
				}
			}

			return target;
		}
	}
}
