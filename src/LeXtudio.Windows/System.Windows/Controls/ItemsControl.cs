using System.Collections;
using System.Linq;

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
    private Microsoft.UI.Xaml.Controls.Panel? _itemsHostPanel;

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
    // direct-panel approach that lays items out horizontally and provides an
    // overflow chevron + popup for items that do not fit.
    internal void UseHorizontalPanelHost()
    {
        if (_shimItems is not null) return; // already initialised

        var panel = new Primitives.ToolBarOverflowAwarePanel();

        // Overflow chevron: a Button with a Flyout. Flyout auto-positions relative
        // to the button and renders reliably on Uno desktop (a bare Popup does not).
        var chevron = new Microsoft.UI.Xaml.Controls.Button
        {
            Content = "»",
            FontSize = 12,
            Padding = new Thickness(4, 0, 4, 0),
            MinWidth = 0,
            VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Stretch,
            Visibility = Microsoft.UI.Xaml.Visibility.Collapsed,
            Name = "ToolBarOverflowButton"
        };
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(chevron, "ToolBarOverflowButton");
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(chevron, "Overflow");

        var popupHost = new Microsoft.UI.Xaml.Controls.StackPanel
        {
            Orientation = Microsoft.UI.Xaml.Controls.Orientation.Vertical
        };
        var flyout = new Microsoft.UI.Xaml.Controls.Flyout
        {
            Content = popupHost
        };
        // Strip the default FlyoutPresenter min-width/padding so the dropdown
        // hugs the toolbar icons instead of being ~300px wide.
        var presenterStyle = new Microsoft.UI.Xaml.Style(typeof(Microsoft.UI.Xaml.Controls.FlyoutPresenter));
        presenterStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(Microsoft.UI.Xaml.FrameworkElement.MinWidthProperty, 0.0));
        presenterStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(Microsoft.UI.Xaml.Controls.Control.MaxWidthProperty, double.PositiveInfinity));
        presenterStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(Microsoft.UI.Xaml.Controls.Control.PaddingProperty, new Thickness(2)));
        presenterStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(Microsoft.UI.Xaml.FrameworkElement.MinHeightProperty, 0.0));
        flyout.FlyoutPresenterStyle = presenterStyle;
        try { flyout.Placement = Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.BottomEdgeAlignedRight; }
        catch { /* placement best-effort */ }
        // Open explicitly on click rather than via Button.Flyout: ShowAt fires for
        // both real clicks and reflection-invoked clicks (DevFlow), and positions
        // the flyout relative to the chevron.
        chevron.Click += (_, _) => flyout.ShowAt(chevron);

        var grid = new Microsoft.UI.Xaml.Controls.Grid();
        grid.ColumnDefinitions.Add(new Microsoft.UI.Xaml.Controls.ColumnDefinition
        {
            Width = new Microsoft.UI.Xaml.GridLength(1, Microsoft.UI.Xaml.GridUnitType.Star)
        });
        grid.ColumnDefinitions.Add(new Microsoft.UI.Xaml.Controls.ColumnDefinition
        {
            Width = Microsoft.UI.Xaml.GridLength.Auto
        });
        Microsoft.UI.Xaml.Controls.Grid.SetColumn(panel, 0);
        Microsoft.UI.Xaml.Controls.Grid.SetColumn(chevron, 1);
        grid.Children.Add(panel);
        grid.Children.Add(chevron);

        var suspendOverflowSync = false;

        panel.OverflowChanged += (_, _) =>
        {
            if (suspendOverflowSync) return;
            chevron.Visibility = panel.OverflowItems.Count > 0
                ? Microsoft.UI.Xaml.Visibility.Visible
                : Microsoft.UI.Xaml.Visibility.Collapsed;
        };

        flyout.Opening += (_, _) =>
        {
            suspendOverflowSync = true;
            popupHost.Children.Clear();
            foreach (var item in panel.OverflowItems.ToList())
            {
                panel.Children.Remove(item);
                popupHost.Children.Add(item);
            }
        };

        flyout.Closed += (_, _) =>
        {
            foreach (var item in popupHost.Children.ToList())
            {
                popupHost.Children.Remove(item);
                panel.Children.Add(item);
            }
            suspendOverflowSync = false;
        };

        _itemsHostPanel = panel;
        base.Items.Add(grid);
        _shimItems = new ItemCollection { PanelHost = panel };
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
