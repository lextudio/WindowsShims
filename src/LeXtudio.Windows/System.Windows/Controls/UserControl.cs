namespace System.Windows.Controls;

/// <summary>
/// WPF UserControl shim — maps to the WinUI UserControl base so that WPF-derived
/// user controls (e.g. DecompilerTextView) can be linked and still participate in the
/// WinUI visual tree. WPF-specific API on UserControl is provided via extension members
/// in WindowsShims where needed.
/// </summary>
public class UserControl : Microsoft.UI.Xaml.Controls.UserControl
{
}
