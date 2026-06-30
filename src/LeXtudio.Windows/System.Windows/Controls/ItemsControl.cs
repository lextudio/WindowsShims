using System.Collections;

namespace System.Windows.Controls;

// WPF System.Windows.Controls.ItemsControl shim.
// Inherits WinUI ItemsControl and adds WPF-internal helpers needed by HeaderedItemsControl
// and other linked WPF source files. Declared partial to extend ItemsControlSpine.cs.
public partial class ItemsControl : Microsoft.UI.Xaml.Controls.ItemsControl
{
    // ── WPF ControlBoolFlags storage (same as Control shim) ─────────────────
    private ControlBoolFlags _controlBoolField;
    internal bool ReadControlFlag(ControlBoolFlags reqFlag) => (_controlBoolField & reqFlag) != 0;
    internal void WriteControlFlag(ControlBoolFlags reqFlag, bool set)
    {
        if (set) _controlBoolField |= reqFlag;
        else _controlBoolField &= ~reqFlag;
    }

    // ── WPF-internal preparation/cleanup methods ─────────────────────────────
    internal void PrepareItemsControl(object item, ItemsControl parentItemsControl) { }
    internal void ClearItemsControl(object item) { }

    // ── WPF HasNonDefaultValue ────────────────────────────────────────────────
    internal bool HasNonDefaultValue(DependencyProperty dp)
        => ReadLocalValue(dp) != Microsoft.UI.Xaml.DependencyProperty.UnsetValue;

    // ── WPF GetPlainText (for ToString) ──────────────────────────────────────
    internal virtual string GetPlainText() => string.Empty;

    // ── WPF LogicalChildren (no logical tree in WinUI) ───────────────────────
    protected internal virtual IEnumerator LogicalChildren
        => System.Linq.Enumerable.Empty<object>().GetEnumerator();

    // ── WPF DTypeThemeStyleKey stub ───────────────────────────────────────────
    internal virtual DependencyObjectType DTypeThemeStyleKey
        => DependencyObjectType.FromSystemTypeInternal(GetType());

    // ── Shadow Items to return the WPF shim ItemCollection ──────────────────
    // WinUI ItemsControl.Items returns Microsoft.UI.Xaml.Controls.ItemCollection
    // which lacks SortDescriptions/GroupDescriptions. Shadow it so DataGrid and
    // other linked WPF code get our System.Windows.Controls.ItemCollection.
    private ItemCollection? _shimItems;
    private Microsoft.UI.Xaml.Controls.StackPanel? _itemsHostPanel;

    public new ItemCollection Items
    {
        get
        {
            if (_shimItems is null)
            {
                _shimItems = new ItemCollection { WinUIItems = base.Items };
            }
            return _shimItems;
        }
    }

    // Called by ToolBar to replace the WinUI-items forwarding with a
    // direct-panel approach, which gives guaranteed horizontal layout.
    internal void UseHorizontalPanelHost()
    {
        if (_shimItems is not null) return; // already initialised
        _itemsHostPanel = new Microsoft.UI.Xaml.Controls.StackPanel
        {
            Orientation = Microsoft.UI.Xaml.Controls.Orientation.Horizontal
        };
        base.Items.Add(_itemsHostPanel);
        _shimItems = new ItemCollection { PanelHost = _itemsHostPanel };
    }

    // ── WPF-only stub properties not in WinUI ItemsControl ───────────────────
    public string? ItemStringFormat { get; set; }
    public int AlternationCount { get; set; }
}

// Minimal stub for WPF-only BindingGroup (row-level validation in DataGrid).
public class BindingGroup
{
    public System.Collections.IList Items { get; } = new System.Collections.ArrayList();
    public System.Collections.ObjectModel.Collection<System.Windows.Controls.ValidationRule> ValidationRules { get; }
        = new System.Collections.ObjectModel.Collection<System.Windows.Controls.ValidationRule>();
    public bool SharesProposedValues { get; set; }
    public bool ValidateWithoutUpdate() => true;
    public bool BeginEdit() => true;
    public bool CommitEdit() => true;
    public void CancelEdit() { }
}
