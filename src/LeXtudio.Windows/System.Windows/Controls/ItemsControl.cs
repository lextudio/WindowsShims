using System.Collections;

namespace System.Windows.Controls;

// WPF System.Windows.Controls.ItemsControl shim.
// Inherits WinUI ItemsControl and adds WPF-internal helpers needed by HeaderedItemsControl
// and other linked WPF source files.
public class ItemsControl : Microsoft.UI.Xaml.Controls.ItemsControl
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
    // On HAS_UNO these are no-ops; actual data binding is handled by WinUI's ItemsControl.
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

    // ── WPF CoerceValue ───────────────────────────────────────────────────────
    internal void CoerceValue(DependencyProperty dp) { }

    // ── WPF DependencyPropertyKey SetValue path ───────────────────────────────
    public void SetValue(System.Windows.DependencyPropertyKey key, object? value)
        => SetValue(key.DependencyProperty, value);

    // ── WPF SetCurrentValueInternal ───────────────────────────────────────────
    internal void SetCurrentValueInternal(DependencyProperty dp, object? value) => SetValue(dp, value);

    // ── WPF SetResourceReference (WinUI uses SetValue; no resource reference system) ──
    public void SetResourceReference(DependencyProperty dp, object resourceKey) { }

    // ── WPF DTypeThemeStyleKey stub ───────────────────────────────────────────
    internal virtual DependencyObjectType DTypeThemeStyleKey
        => DependencyObjectType.FromSystemTypeInternal(GetType());

    // ── WPF-only stub properties not in WinUI ItemsControl ───────────────────
    public string? ItemStringFormat { get; set; }
    public StyleSelector? ItemContainerStyleSelector { get; set; }
    public int AlternationCount { get; set; }
    public BindingGroup? ItemBindingGroup { get; set; }
}

// Minimal stub for WPF-only types referenced by HeaderedItemsControl.PrepareHierarchy.
public class StyleSelector { }
public class BindingGroup { }
