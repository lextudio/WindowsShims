using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using SelectedCellsChangedEventArgs = System.Windows.Controls.SelectedCellsChangedEventArgs;
using SelectionChangedEventArgs = System.Windows.Controls.SelectionChangedEventArgs;

namespace System.Windows.Automation.Peers
{
    // Weaved into Uno 6.6's native automation: DataGrid's WPF-shaped
    // OnCreateAutomationPeer override (linked upstream source) returns this peer,
    // which is a Microsoft.UI.Xaml.Automation.Peers.AutomationPeer descendant
    // derived through UIElementAutomationPeer (Uno FrameworkElementAutomationPeer).
    // Uno's Skia accessibility walks the visual tree and resolves each element's
    // peer; roles/names come from the Core overrides below
    // and selection/value changes flow via IAutomationPeerListener events.
    public class DataGridAutomationPeer : UIElementAutomationPeer, ISelectionProvider, IGridProvider
    {
        private readonly Dictionary<object, DataGridItemAutomationPeer> _itemPeers = new();

        public DataGridAutomationPeer(Controls.DataGrid owner)
            : base(owner)
        {
        }

        internal Controls.DataGrid OwningDataGrid => (Controls.DataGrid)base.Owner;

        /// <summary>WPF helper: item peer cache for the PropertyChanged paths in linked DataGrid.cs.</summary>
        internal AutomationPeer? FindOrCreateItemAutomationPeer(object? item)
        {
            if (item is null)
            {
                return null;
            }

            if (_itemPeers.TryGetValue(item, out var cached))
            {
                return cached;
            }

            var peer = new DataGridItemAutomationPeer(item, this);
            _itemPeers[item] = peer;
            return peer;
        }

        // ── Uno Core surface ─────────────────────────────────────────────────

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.DataGrid;

        protected override string GetClassNameCore() => nameof(Controls.DataGrid);

        protected override string GetNameCore() => CellAutomationHelper.GetName(OwningDataGrid);

        protected override object? GetPatternCore(PatternInterface patternInterface) =>
            patternInterface switch
            {
                PatternInterface.Selection => this,
                PatternInterface.Grid => this,
                PatternInterface.Scroll => ResolveScrollProvider(),
                _ => base.GetPatternCore(patternInterface),
            };

        private object? ResolveScrollProvider()
        {
            if (OwningDataGrid.ShimGetRowsScrollViewer() is { } scrollViewer &&
                FromElement(scrollViewer) is { } scrollPeer)
            {
                scrollPeer.EventsSource = this;
                return scrollPeer;
            }

            return null;
        }

        // ── ISelectionProvider ───────────────────────────────────────────────

        public bool CanSelectMultiple => OwningDataGrid.SelectionMode is DataGridSelectionMode.Extended;

        public bool IsSelectionRequired => false;

        public IRawElementProviderSimple[] GetSelection() =>
            RealizedRows()
                .Where(row => row.IsSelected)
                .Select(row => FromElement(row))
                .OfType<DataGridRowAutomationPeer>()
                .Select(ProviderFromPeer)
                .ToArray()!;

        // ── IGridProvider ────────────────────────────────────────────────────

        public int RowCount => RealizedRows().Count();

        public int ColumnCount => OwningDataGrid.Columns.Count;

        public IRawElementProviderSimple GetItem(int row, int column)
        {
            var realizedRows = RealizedRows().ToList();
            if (row >= 0 && row < realizedRows.Count)
            {
                var cellPeer = realizedRows[row]
                    .EffectiveCells()
                    .FirstOrDefault(cell => cell.Column?.DisplayIndex == column) is { } cell
                        ? FromElement(cell) as DataGridCellAutomationPeer
                        : null;
                if (cellPeer is not null)
                {
                    return ProviderFromPeer(cellPeer);
                }
            }

            return null!;
        }

        // ── WPF internal raise sites (called from linked DataGrid.cs) ────────

        internal void RaiseAutomationRowInvokeEvents(DataGridRow row)
        {
            if (ListenerExists(AutomationEvents.InvokePatternOnInvoked) &&
                FromElement(row) is { } rowPeer)
            {
                rowPeer.EventsSource = this;
                rowPeer.RaiseAutomationEvent(AutomationEvents.InvokePatternOnInvoked);
            }
        }

        internal void RaiseAutomationCellInvokeEvents(DataGridColumn column, DataGridRow row)
        {
            if (ListenerExists(AutomationEvents.InvokePatternOnInvoked) &&
                row.EffectiveCells().FirstOrDefault(cell => cell.Column == column) is { } cell &&
                FromElement(cell) is { } cellPeer)
            {
                cellPeer.EventsSource = this;
                cellPeer.RaiseAutomationEvent(AutomationEvents.InvokePatternOnInvoked);
            }
        }

        internal void RaiseAutomationCellSelectedEvent(SelectedCellsChangedEventArgs e)
        {
            if (!ListenerExists(AutomationEvents.SelectionItemPatternOnElementSelected))
            {
                return;
            }

            var raised = false;
            foreach (var cellInfo in e.AddedCells)
            {
                if (FindRealizedCellPeer(cellInfo) is { } cellPeer)
                {
                    cellPeer.EventsSource = this;
                    cellPeer.RaiseAutomationEvent(AutomationEvents.SelectionItemPatternOnElementSelected);
                    raised = true;
                }
            }

            if (!raised)
            {
                RaiseAutomationEvent(AutomationEvents.SelectionPatternOnInvalidated);
            }
        }

        internal void RaiseAutomationSelectionEvents(SelectionChangedEventArgs e)
        {
            if (!ListenerExists(AutomationEvents.SelectionItemPatternOnElementSelected))
            {
                return;
            }

            var raised = false;
            foreach (var item in e.AddedItems)
            {
                if (FindRealizedRowPeer(item) is { } rowPeer)
                {
                    rowPeer.EventsSource = this;
                    rowPeer.RaiseAutomationEvent(AutomationEvents.SelectionItemPatternOnElementAddedToSelection);
                    raised = true;
                }
            }

            foreach (var item in e.RemovedItems)
            {
                if (FindRealizedRowPeer(item) is { } rowPeer)
                {
                    rowPeer.EventsSource = this;
                    rowPeer.RaiseAutomationEvent(AutomationEvents.SelectionItemPatternOnElementRemovedFromSelection);
                    raised = true;
                }
            }

            if (!raised)
            {
                RaiseAutomationEvent(AutomationEvents.SelectionPatternOnInvalidated);
            }
        }

        // ── helpers ──────────────────────────────────────────────────────────

        private IEnumerable<DataGridRow> RealizedRows() =>
            OwningDataGrid.ItemContainerGenerator.Containers.OfType<DataGridRow>();

        private DataGridRowAutomationPeer? FindRealizedRowPeer(object item) =>
            RealizedRows().FirstOrDefault(row => ReferenceEquals(row.Item, item)) is { } row
                ? FromElement(row) as DataGridRowAutomationPeer
                : null;

        private DataGridCellAutomationPeer? FindRealizedCellPeer(DataGridCellInfo cellInfo) =>
            RealizedRows().FirstOrDefault(row => ReferenceEquals(row.Item, cellInfo.Item)) is { } row &&
            row.EffectiveCells().FirstOrDefault(cell => cell.Column == cellInfo.Column) is { } cell
                ? FromElement(cell) as DataGridCellAutomationPeer
                : null;
    }

    /// <summary>Item-level peer (no element owner): bridges PropertyChanged routing to the realized row/cell element peers.</summary>
    public class DataGridItemAutomationPeer : AutomationPeer
    {
        private readonly object _item;
        private readonly DataGridAutomationPeer _owner;
        private readonly Dictionary<DataGridColumn, DataGridCellItemAutomationPeer> _cellPeers = new();

        internal DataGridItemAutomationPeer(object item, DataGridAutomationPeer owner)
        {
            _item = item;
            _owner = owner;
        }

        internal DataGridAutomationPeer OwnerPeer => _owner;

        internal DataGridCellItemAutomationPeer? GetOrCreateCellItemPeer(DataGridColumn column)
        {
            if (!_cellPeers.TryGetValue(column, out var peer))
            {
                peer = new DataGridCellItemAutomationPeer(this, column);
                _cellPeers[column] = peer;
            }

            return peer;
        }

        // Called from linked DataGridRow.cs selection-change path.
        public new void RaisePropertyChangedEvent(AutomationProperty property, object? oldValue, object? newValue)
        {
            if (property.UnoProperty is { } unoProperty && FindRowPeer() is { } rowPeer)
            {
                rowPeer.EventsSource = _owner;
                rowPeer.RaisePropertyChangedEvent(property, oldValue, newValue);
            }
        }

        private DataGridRowAutomationPeer? FindRowPeer()
        {
            var realized = _owner.OwningDataGrid.ItemContainerGenerator.Containers.OfType<DataGridRow>();
            foreach (var row in realized)
            {
                if (ReferenceEquals(row.Item, _item))
                {
                    return FromElement(row) as DataGridRowAutomationPeer;
                }
            }

            return null;
        }
    }

    public class DataGridCellItemAutomationPeer : AutomationPeer
    {
        private readonly DataGridItemAutomationPeer _owner;
        private readonly DataGridColumn _column;

        internal DataGridCellItemAutomationPeer(DataGridItemAutomationPeer owner, DataGridColumn column)
        {
            _owner = owner;
            _column = column;
        }

        // Called from linked DataGrid.cs CellAutomationValueHolder.TrackValue path.
        public new void RaisePropertyChangedEvent(AutomationProperty property, object? oldValue, object? newValue)
        {
            if (property.UnoProperty is { } unoProperty && FindCellPeer() is { } cellPeer)
            {
                cellPeer.RaisePropertyChangedEvent(property, oldValue, newValue);
            }
        }

        private DataGridCellAutomationPeer? FindCellPeer()
        {
            var grid = _owner.OwnerPeer.OwningDataGrid;
            foreach (var row in grid.ItemContainerGenerator.Containers.OfType<DataGridRow>())
            {
                foreach (var cell in row.EffectiveCells())
                {
                    if (cell.Column == _column)
                    {
                        return FromElement(cell) as DataGridCellAutomationPeer;
                    }
                }
            }

            return null;
        }
    }

    /// <summary>Row container peer — element-backed, so Uno routes selection events to the row's native element.</summary>
    public class DataGridRowAutomationPeer : UIElementAutomationPeer, ISelectionItemProvider
    {
        public DataGridRowAutomationPeer(DataGridRow owner)
            : base(owner)
        {
        }

        public new DataGridRow Owner => (DataGridRow)base.Owner;

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.DataItem;

        protected override string GetNameCore() => CellAutomationHelper.GetRowName(Owner);

        protected override object? GetPatternCore(PatternInterface patternInterface) =>
            patternInterface == PatternInterface.SelectionItem ? this : base.GetPatternCore(patternInterface);

        // ── ISelectionItemProvider ───────────────────────────────────────────

        public bool IsSelected => Owner.IsSelected;

        public IRawElementProviderSimple? SelectionContainer =>
            Owner.DataGridOwner is { } grid ? ProviderFromPeer(FromElement(grid)) : null;

        public void AddToSelection() => Owner.IsSelected = true;

        public void RemoveFromSelection() => Owner.IsSelected = false;

        public void Select() => Owner.IsSelected = true;
    }
}