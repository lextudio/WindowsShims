namespace System.Windows;

// WPF SystemResourceKey — typed resource key used by ToolBar to identify per-item default styles
// and DataGrid system-defined brushes. Inherits ComponentResourceKey so the shim instances can
// be assigned to ComponentResourceKey-typed fields in linked DataGrid/ToolBar source.
public sealed class SystemResourceKey : ComponentResourceKey
{
    private SystemResourceKey(string name) : base(typeof(SystemResourceKey), name) { }

    public static readonly SystemResourceKey ToolBarButtonStyleKey       = new("ToolBarButtonStyle");
    public static readonly SystemResourceKey ToolBarToggleButtonStyleKey = new("ToolBarToggleButtonStyle");
    public static readonly SystemResourceKey ToolBarSeparatorStyleKey    = new("ToolBarSeparatorStyle");
    public static readonly SystemResourceKey ToolBarCheckBoxStyleKey     = new("ToolBarCheckBoxStyle");
    public static readonly SystemResourceKey ToolBarRadioButtonStyleKey  = new("ToolBarRadioButtonStyle");
    public static readonly SystemResourceKey ToolBarComboBoxStyleKey     = new("ToolBarComboBoxStyle");
    public static readonly SystemResourceKey ToolBarTextBoxStyleKey      = new("ToolBarTextBoxStyle");
    public static readonly SystemResourceKey ToolBarMenuStyleKey         = new("ToolBarMenuStyle");

    // DataGrid resource keys (formerly in DataGridHelperStubs.cs).
    public static readonly SystemResourceKey DataGridFocusBorderBrushKey                          = new("DataGridFocusBorderBrush");
    public static readonly SystemResourceKey DataGridComboBoxColumnTextBlockComboBoxStyleKey       = new("DataGridComboBoxColumnTextBlockComboBoxStyle");
    public static readonly SystemResourceKey DataGridColumnHeaderColumnHeaderDropSeparatorStyleKey  = new("DataGridColumnHeaderColumnHeaderDropSeparatorStyle");
    public static readonly SystemResourceKey DataGridColumnHeaderColumnFloatingHeaderStyleKey      = new("DataGridColumnHeaderColumnFloatingHeaderStyle");
}
