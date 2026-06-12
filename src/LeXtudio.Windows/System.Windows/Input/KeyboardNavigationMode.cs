namespace System.Windows.Input;

/// <summary>Compiler shim mirroring WPF's keyboard navigation modes.</summary>
public enum KeyboardNavigationMode
{
    Continue,
    Once,
    Cycle,
    None,
    Contained,
    Local,
}
