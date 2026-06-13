namespace System.Windows.Input;

// Session 60: input surface the linked DataGridCheckBoxColumn/DataGridComboBoxColumn
// edit-trigger code (OnInput, PrepareCellForEdit) references. These paths are not
// invoked by the shim render path (cells handle their own input), so the shims
// only need to compile and report "no hit / not down".
[Flags]
public enum KeyStates
{
    None = 0,
    Down = 1,
    Toggled = 2,
}

public static class WinUIInputHitTestExtensions
{
    public static Microsoft.UI.Xaml.UIElement? InputHitTest(
        this Microsoft.UI.Xaml.UIElement element, Point point) => null;
}
