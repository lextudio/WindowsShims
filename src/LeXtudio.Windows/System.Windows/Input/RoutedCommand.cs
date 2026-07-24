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
			return CanExecute(parameter, null);
		}

		public void Execute(object parameter)
		{
			Execute(parameter, null);
		}

        public bool CanExecute(object parameter, object? target)
        {
            if (_bindings.Count == 0)
            {
                return true;
            }

            foreach (CommandBinding binding in _bindings)
            {
                if (!binding.AppliesTo(target))
                {
                    continue;
                }

                var invocationTarget = binding.ResolveInvocationTarget(target);

                var args = new CanExecuteRoutedEventArgs(this, parameter)
                {
                    Source = target,
                    OriginalSource = target,
                };
                binding.OnCanExecute(invocationTarget, args);
                if (args.ContinueRouting)
                {
                    // Handler asked to continue routing — try the next binding.
                    continue;
                }
                // Return the handler's decision (whether or not it set
                // args.Handled; many WPF handlers only set CanExecute).
                return args.CanExecute;
            }

            // No binding claimed this command.
            return false;
        }

        public void Execute(object parameter, object? target)
        {
            foreach (CommandBinding binding in _bindings)
            {
                if (!binding.AppliesTo(target))
                {
                    continue;
                }

                var invocationTarget = binding.ResolveInvocationTarget(target);

                var args = new ExecutedRoutedEventArgs(this, parameter)
                {
                    Source = target,
                    OriginalSource = target,
                };
                binding.OnExecuted(invocationTarget, args);
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
