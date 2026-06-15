namespace System.Windows.Controls;

/// <summary>
/// WPF ListBox shim — minimal surface for ContextMenuEntry and similar call sites that
/// accept a ListBox in context-menu wiring. Roma does not use ListBox in its UI path;
/// this stub satisfies the type hierarchy.
/// </summary>
public enum SelectionMode { Single, Multiple, Extended }

public class ListBox : ItemsControl
{
    public object? SelectedItem { get; set; }

    public static readonly DependencyProperty SelectionModeProperty =
        DependencyProperty.Register(nameof(SelectionMode), typeof(SelectionMode), typeof(ListBox), null);

    public SelectionMode SelectionMode
    {
        get => (SelectionMode)GetValue(SelectionModeProperty);
        set => SetValue(SelectionModeProperty, value);
    }
}
