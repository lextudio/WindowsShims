using System;
using System.Collections.Generic;

namespace System.Windows.Input
{
	/// <summary>
	/// Compiler shim for <c>System.Windows.Input.RoutedCommand</c>.
	/// Preserves the command name for diagnostics; does not perform WPF routing.
	/// </summary>
	public class RoutedCommand : ICommand
	{
		public string Name { get; }
		public Type OwnerType { get; }
		public InputGestureCollection InputGestures { get; }
		private readonly List<CommandBinding> _bindings = new List<CommandBinding>();

		public RoutedCommand(string name, Type ownerType)
		{
			Name = name ?? string.Empty;
			OwnerType = ownerType;
			InputGestures = new InputGestureCollection();
		}

		public RoutedCommand(string name, Type ownerType, InputGestureCollection inputGestures)
		{
			Name = name ?? string.Empty;
			OwnerType = ownerType;
			InputGestures = inputGestures ?? new InputGestureCollection();
		}

		internal void RegisterBinding(CommandBinding binding)
		{
			if (binding != null && !_bindings.Contains(binding))
			{
				_bindings.Add(binding);
			}
		}

		public bool CanExecute(object parameter)
		{
			if (_bindings.Count == 0)
			{
				return true;
			}

			bool anyHandled = false;
			bool canExecute = true;
			foreach (CommandBinding binding in _bindings)
			{
				var args = new CanExecuteRoutedEventArgs(this, parameter);
				binding.OnCanExecute(args);
				if (args.Handled)
				{
					anyHandled = true;
					canExecute = args.CanExecute;
					if (!args.ContinueRouting)
					{
						return args.CanExecute;
					}
				}
			}

			return !anyHandled || canExecute;
		}

		public void Execute(object parameter)
		{
			foreach (CommandBinding binding in _bindings)
			{
				var args = new ExecutedRoutedEventArgs(this, parameter);
				binding.OnExecuted(args);
				if (args.Handled)
				{
					return;
				}
			}
		}
#pragma warning disable 67
		public event EventHandler CanExecuteChanged { add { } remove { } }
#pragma warning restore 67
	}
}
