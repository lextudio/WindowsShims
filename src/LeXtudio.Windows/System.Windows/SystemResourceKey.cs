namespace System.Windows;

// WPF SystemResourceKey — typed resource key used by ToolBar to identify per-item default styles
// (e.g. ToolBarButtonStyleKey). On HAS_UNO these are just object-identity keys looked up in
// the application resource dictionary via FrameworkElement.SetResourceReference.
public abstract class ResourceKey { }

public sealed class SystemResourceKey : ResourceKey
{
    private readonly string _name;
    private SystemResourceKey(string name) => _name = name;
    public override string ToString() => _name;

    public static readonly SystemResourceKey ToolBarButtonStyleKey       = new("ToolBarButtonStyle");
    public static readonly SystemResourceKey ToolBarToggleButtonStyleKey = new("ToolBarToggleButtonStyle");
    public static readonly SystemResourceKey ToolBarSeparatorStyleKey    = new("ToolBarSeparatorStyle");
    public static readonly SystemResourceKey ToolBarCheckBoxStyleKey     = new("ToolBarCheckBoxStyle");
    public static readonly SystemResourceKey ToolBarRadioButtonStyleKey  = new("ToolBarRadioButtonStyle");
    public static readonly SystemResourceKey ToolBarComboBoxStyleKey     = new("ToolBarComboBoxStyle");
    public static readonly SystemResourceKey ToolBarTextBoxStyleKey      = new("ToolBarTextBoxStyle");
    public static readonly SystemResourceKey ToolBarMenuStyleKey         = new("ToolBarMenuStyle");
}
