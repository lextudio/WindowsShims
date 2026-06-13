using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace System.Windows.Controls;

internal sealed class DataGridColumnCollection : ObservableCollection<DataGridColumn>
{
    private readonly DataGrid _dataGridOwner;
    private bool _displayIndexMapInitialized;
    private bool _isUpdatingDisplayIndex;
    private bool _isClearingDisplayIndex;

    internal DataGridColumnCollection(DataGrid dataGridOwner)
    {
        _dataGridOwner = dataGridOwner ?? throw new ArgumentNullException(nameof(dataGridOwner));
        DisplayIndexMap = new List<int>(5);
    }

    internal DataGrid DataGridOwner => _dataGridOwner;

    internal bool DisplayIndexMapInitialized => _displayIndexMapInitialized;

    internal List<int> DisplayIndexMap { get; }

    internal DataGridColumn ColumnFromDisplayIndex(int displayIndex)
    {
        if (displayIndex < 0 || displayIndex >= DisplayIndexMap.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(displayIndex));
        }

        return this[DisplayIndexMap[displayIndex]];
    }

    internal int ColumnIndexFromDisplayIndex(int displayIndex)
    {
        if (displayIndex < 0 || displayIndex >= DisplayIndexMap.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(displayIndex));
        }

        return DisplayIndexMap[displayIndex];
    }

    protected override void InsertItem(int index, DataGridColumn item)
    {
        ValidateNewColumn(item);

        if (DisplayIndexMapInitialized)
        {
            ValidateDisplayIndex(item, item.DisplayIndex, isAdding: true);
        }

        base.InsertItem(index, item);
        item.CoerceValue(DataGridColumn.IsFrozenProperty);
    }

    protected override void SetItem(int index, DataGridColumn item)
    {
        ValidateNewColumn(item, this[index]);

        if (DisplayIndexMapInitialized)
        {
            ValidateDisplayIndex(item, item.DisplayIndex);
        }

        base.SetItem(index, item);
        item.CoerceValue(DataGridColumn.IsFrozenProperty);
    }

    protected override void ClearItems()
    {
        ClearDisplayIndex(this, null);
        DataGridOwner.UpdateDataGridReference(this, clear: true);
        base.ClearItems();
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (DisplayIndexMapInitialized)
                {
                    UpdateDisplayIndexForNewColumns(e.NewItems!, e.NewStartingIndex);
                }

                InvalidateHasVisibleStarColumns();
                break;

            case NotifyCollectionChangedAction.Move:
                if (DisplayIndexMapInitialized)
                {
                    UpdateDisplayIndexForMovedColumn(e.OldStartingIndex, e.NewStartingIndex);
                }

                break;

            case NotifyCollectionChangedAction.Remove:
                if (DisplayIndexMapInitialized)
                {
                    UpdateDisplayIndexForRemovedColumns(e.OldItems!, e.OldStartingIndex);
                }

                ClearDisplayIndex(e.OldItems!, e.NewItems);
                InvalidateHasVisibleStarColumns();
                break;

            case NotifyCollectionChangedAction.Replace:
                if (DisplayIndexMapInitialized)
                {
                    UpdateDisplayIndexForReplacedColumn(e.OldItems!, e.NewItems!);
                }

                ClearDisplayIndex(e.OldItems!, e.NewItems);
                InvalidateHasVisibleStarColumns();
                break;

            case NotifyCollectionChangedAction.Reset:
                if (DisplayIndexMapInitialized)
                {
                    DisplayIndexMap.Clear();
                    DataGridOwner.UpdateColumnsOnVirtualizedCellInfoCollections(
                        NotifyCollectionChangedAction.Reset, -1, null, -1);
                }

                InvalidateHasVisibleStarColumns();
                break;
        }

        InvalidateAverageColumnWidth();
        base.OnCollectionChanged(e);
    }

    internal void NotifyPropertyChanged(
        DependencyObject dependencyObject,
        string propertyName,
        DependencyPropertyChangedEventArgs args,
        DataGridNotificationTarget target)
    {
        if (DataGridHelper.ShouldNotifyColumnCollection(target))
        {
            if (args.Property == DataGridColumn.DisplayIndexProperty)
            {
                OnColumnDisplayIndexChanged(
                    (DataGridColumn)dependencyObject,
                    (int)args.OldValue,
                    (int)args.NewValue);

                if (((DataGridColumn)dependencyObject).IsVisible)
                {
                    InvalidateColumnRealization(true);
                }
            }
            else if (args.Property == DataGridColumn.WidthProperty)
            {
                if (((DataGridColumn)dependencyObject).IsVisible)
                {
                    InvalidateColumnRealization(false);
                }
            }
            else if (args.Property == DataGrid.FrozenColumnCountProperty)
            {
                InvalidateColumnRealization(false);
                OnDataGridFrozenColumnCountChanged((int)args.OldValue, (int)args.NewValue);
            }
            else if (args.Property == DataGridColumn.VisibilityProperty)
            {
                InvalidateAverageColumnWidth();
                InvalidateHasVisibleStarColumns();
                InvalidateColumnWidthsComputation();
                InvalidateColumnRealization(true);
            }
            else if (args.Property == DataGrid.EnableColumnVirtualizationProperty)
            {
                InvalidateColumnRealization(true);
            }
            else if (args.Property == DataGrid.CellsPanelHorizontalOffsetProperty ||
                     args.Property == DataGrid.HorizontalScrollOffsetProperty ||
                     string.Equals(propertyName, "ViewportWidth", StringComparison.Ordinal))
            {
                InvalidateColumnRealization(false);
            }
        }

        if (DataGridHelper.ShouldNotifyColumns(target))
        {
            foreach (var column in this)
            {
                column.NotifyPropertyChanged(dependencyObject, args, DataGridNotificationTarget.Columns);
            }
        }
    }

    private void ValidateNewColumn(DataGridColumn item, DataGridColumn? currentItem = null)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item), SR.DataGrid_NullColumn);
        }

        if (item.DataGridOwner is not null && !ReferenceEquals(item, currentItem))
        {
            throw new ArgumentException(SR.Format(SR.DataGrid_InvalidColumnReuse, item.Header), nameof(item));
        }
    }

    private int CoerceDefaultDisplayIndex(DataGridColumn column)
        => CoerceDefaultDisplayIndex(column, IndexOf(column));

    private int CoerceDefaultDisplayIndex(DataGridColumn column, int newDisplayIndex)
    {
        if (DataGridHelper.IsDefaultValue(column, DataGridColumn.DisplayIndexProperty))
        {
            var wasUpdating = _isUpdatingDisplayIndex;
            try
            {
                _isUpdatingDisplayIndex = true;
                column.DisplayIndex = newDisplayIndex;
            }
            finally
            {
                _isUpdatingDisplayIndex = wasUpdating;
            }

            return newDisplayIndex;
        }

        return column.DisplayIndex;
    }

    private void OnColumnDisplayIndexChanged(DataGridColumn column, int oldDisplayIndex, int newDisplayIndex)
    {
        var originalOldDisplayIndex = oldDisplayIndex;
        if (!DisplayIndexMapInitialized)
        {
            InitializeDisplayIndexMap(column, oldDisplayIndex, out oldDisplayIndex);
        }

        if (_isClearingDisplayIndex)
        {
            return;
        }

        newDisplayIndex = CoerceDefaultDisplayIndex(column);
        if (newDisplayIndex == oldDisplayIndex)
        {
            return;
        }

        ValidateDisplayIndex(column, newDisplayIndex);

        if (originalOldDisplayIndex != -1)
        {
            DataGridOwner.OnColumnDisplayIndexChanged(new DataGridColumnEventArgs(column));
        }

        UpdateDisplayIndexForChangedColumn(oldDisplayIndex, newDisplayIndex);
    }

    private void UpdateDisplayIndexForChangedColumn(int oldDisplayIndex, int newDisplayIndex)
    {
        if (_isUpdatingDisplayIndex)
        {
            return;
        }

        try
        {
            _isUpdatingDisplayIndex = true;

            var columnIndex = DisplayIndexMap[oldDisplayIndex];
            DisplayIndexMap.RemoveAt(oldDisplayIndex);
            DisplayIndexMap.Insert(newDisplayIndex, columnIndex);

            if (newDisplayIndex < oldDisplayIndex)
            {
                for (var i = newDisplayIndex + 1; i <= oldDisplayIndex; i++)
                {
                    ColumnFromDisplayIndex(i).DisplayIndex++;
                }
            }
            else
            {
                for (var i = oldDisplayIndex; i < newDisplayIndex; i++)
                {
                    ColumnFromDisplayIndex(i).DisplayIndex--;
                }
            }

            DataGridOwner.UpdateColumnsOnVirtualizedCellInfoCollections(
                NotifyCollectionChangedAction.Move, oldDisplayIndex, null, newDisplayIndex);
        }
        finally
        {
            _isUpdatingDisplayIndex = false;
        }
    }

    private void UpdateDisplayIndexForMovedColumn(int oldColumnIndex, int newColumnIndex)
    {
        var displayIndex = RemoveFromDisplayIndexMap(oldColumnIndex);
        InsertInDisplayIndexMap(displayIndex, newColumnIndex);
        DataGridOwner.UpdateColumnsOnVirtualizedCellInfoCollections(
            NotifyCollectionChangedAction.Move, oldColumnIndex, null, newColumnIndex);
    }

    private void UpdateDisplayIndexForNewColumns(IList newColumns, int startingIndex)
    {
        try
        {
            _isUpdatingDisplayIndex = true;

            var column = (DataGridColumn)newColumns[0]!;
            var newDisplayIndex = CoerceDefaultDisplayIndex(column, startingIndex);
            InsertInDisplayIndexMap(newDisplayIndex, startingIndex);

            for (var i = 0; i < DisplayIndexMap.Count; i++)
            {
                if (i > newDisplayIndex)
                {
                    ColumnFromDisplayIndex(i).DisplayIndex++;
                }
            }

            DataGridOwner.UpdateColumnsOnVirtualizedCellInfoCollections(
                NotifyCollectionChangedAction.Add, -1, null, newDisplayIndex);
        }
        finally
        {
            _isUpdatingDisplayIndex = false;
        }
    }

    internal void InitializeDisplayIndexMap()
    {
        _displayIndexMapInitialized = false;
        InitializeDisplayIndexMap(null, -1, out _);
    }

    private void InitializeDisplayIndexMap(
        DataGridColumn? changingColumn,
        int oldDisplayIndex,
        out int resultDisplayIndex)
    {
        resultDisplayIndex = oldDisplayIndex;
        if (_displayIndexMapInitialized)
        {
            return;
        }

        _displayIndexMapInitialized = true;
        DisplayIndexMap.Clear();

        var columnCount = Count;
        var assignedDisplayIndexMap = new Dictionary<int, int>();

        if (changingColumn is not null && oldDisplayIndex >= columnCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(oldDisplayIndex),
                oldDisplayIndex,
                SR.Format(SR.DataGrid_ColumnDisplayIndexOutOfRange, changingColumn.Header));
        }

        for (var columnIndex = 0; columnIndex < columnCount; columnIndex++)
        {
            var currentColumn = this[columnIndex];
            var currentDisplayIndex = currentColumn.DisplayIndex;
            ValidateDisplayIndex(currentColumn, currentDisplayIndex);

            if (ReferenceEquals(currentColumn, changingColumn))
            {
                currentDisplayIndex = oldDisplayIndex;
            }

            if (currentDisplayIndex >= 0)
            {
                if (assignedDisplayIndexMap.ContainsKey(currentDisplayIndex))
                {
                    throw new ArgumentException(SR.DataGrid_DuplicateDisplayIndex);
                }

                assignedDisplayIndexMap.Add(currentDisplayIndex, columnIndex);
            }
        }

        var nextAvailableDisplayIndex = 0;
        for (var columnIndex = 0; columnIndex < columnCount; columnIndex++)
        {
            var currentColumn = this[columnIndex];
            var hasDefaultDisplayIndex = DataGridHelper.IsDefaultValue(currentColumn, DataGridColumn.DisplayIndexProperty);

            if (ReferenceEquals(currentColumn, changingColumn) && oldDisplayIndex == -1)
            {
                hasDefaultDisplayIndex = true;
            }

            if (hasDefaultDisplayIndex)
            {
                while (assignedDisplayIndexMap.ContainsKey(nextAvailableDisplayIndex))
                {
                    nextAvailableDisplayIndex++;
                }

                CoerceDefaultDisplayIndex(currentColumn, nextAvailableDisplayIndex);
                assignedDisplayIndexMap.Add(nextAvailableDisplayIndex, columnIndex);

                if (ReferenceEquals(currentColumn, changingColumn))
                {
                    resultDisplayIndex = nextAvailableDisplayIndex;
                }

                nextAvailableDisplayIndex++;
            }
        }

        for (var displayIndex = 0; displayIndex < columnCount; displayIndex++)
        {
            DisplayIndexMap.Add(assignedDisplayIndexMap[displayIndex]);
        }
    }

    private void UpdateDisplayIndexForRemovedColumns(IList oldColumns, int startingIndex)
    {
        try
        {
            _isUpdatingDisplayIndex = true;

            var removedDisplayIndex = RemoveFromDisplayIndexMap(startingIndex);
            for (var i = 0; i < DisplayIndexMap.Count; i++)
            {
                if (i >= removedDisplayIndex)
                {
                    ColumnFromDisplayIndex(i).DisplayIndex--;
                }
            }

            DataGridOwner.UpdateColumnsOnVirtualizedCellInfoCollections(
                NotifyCollectionChangedAction.Remove, removedDisplayIndex, (DataGridColumn)oldColumns[0]!, -1);
        }
        finally
        {
            _isUpdatingDisplayIndex = false;
        }
    }

    private void UpdateDisplayIndexForReplacedColumn(IList oldColumns, IList newColumns)
    {
        var oldColumn = (DataGridColumn)oldColumns[0]!;
        var newColumn = (DataGridColumn)newColumns[0]!;
        var newDisplayIndex = CoerceDefaultDisplayIndex(newColumn);

        if (oldColumn.DisplayIndex != newDisplayIndex)
        {
            UpdateDisplayIndexForChangedColumn(oldColumn.DisplayIndex, newDisplayIndex);
        }

        DataGridOwner.UpdateColumnsOnVirtualizedCellInfoCollections(
            NotifyCollectionChangedAction.Replace, newDisplayIndex, oldColumn, newDisplayIndex);
    }

    private void ClearDisplayIndex(IList? oldColumns, IList? newColumns)
    {
        if (oldColumns is null)
        {
            return;
        }

        try
        {
            _isClearingDisplayIndex = true;
            foreach (DataGridColumn column in oldColumns)
            {
                if (newColumns is not null && newColumns.Contains(column))
                {
                    continue;
                }

                column.ClearValue(DataGridColumn.DisplayIndexProperty);
            }
        }
        finally
        {
            _isClearingDisplayIndex = false;
        }
    }

    private bool IsDisplayIndexValid(DataGridColumn column, int displayIndex, bool isAdding)
    {
        if (displayIndex == -1 && DataGridHelper.IsDefaultValue(column, DataGridColumn.DisplayIndexProperty))
        {
            return true;
        }

        return displayIndex >= 0 && (isAdding ? displayIndex <= Count : displayIndex < Count);
    }

    private void InsertInDisplayIndexMap(int newDisplayIndex, int columnIndex)
    {
        DisplayIndexMap.Insert(newDisplayIndex, columnIndex);

        for (var i = 0; i < DisplayIndexMap.Count; i++)
        {
            if (DisplayIndexMap[i] >= columnIndex && i != newDisplayIndex)
            {
                DisplayIndexMap[i]++;
            }
        }
    }

    private int RemoveFromDisplayIndexMap(int columnIndex)
    {
        var removedDisplayIndex = DisplayIndexMap.IndexOf(columnIndex);
        DisplayIndexMap.RemoveAt(removedDisplayIndex);

        for (var i = 0; i < DisplayIndexMap.Count; i++)
        {
            if (DisplayIndexMap[i] >= columnIndex)
            {
                DisplayIndexMap[i]--;
            }
        }

        return removedDisplayIndex;
    }

    internal void ValidateDisplayIndex(DataGridColumn column, int displayIndex)
    {
        ValidateDisplayIndex(column, displayIndex, isAdding: false);
    }

    internal void ValidateDisplayIndex(DataGridColumn column, int displayIndex, bool isAdding)
    {
        if (!IsDisplayIndexValid(column, displayIndex, isAdding))
        {
            throw new ArgumentOutOfRangeException(
                nameof(displayIndex),
                displayIndex,
                SR.Format(SR.DataGrid_ColumnDisplayIndexOutOfRange, column.Header));
        }
    }

    private void OnDataGridFrozenColumnCountChanged(int oldFrozenCount, int newFrozenCount)
    {
        if (newFrozenCount > oldFrozenCount)
        {
            var columnCount = Math.Min(newFrozenCount, Count);
            for (var i = oldFrozenCount; i < columnCount; i++)
            {
                ColumnFromDisplayIndex(i).IsFrozen = true;
            }
        }
        else
        {
            var columnCount = Math.Min(oldFrozenCount, Count);
            for (var i = newFrozenCount; i < columnCount; i++)
            {
                ColumnFromDisplayIndex(i).IsFrozen = false;
            }
        }
    }

    // Width/realization internals remain local stubs until the layout batch.
    internal bool ColumnWidthsComputationPending { get; set; }
    internal bool HasVisibleStarColumns => this.Any(c => c.IsVisible && c.Width.IsStar);
    internal int FirstVisibleDisplayIndex => this.Where(c => c.IsVisible).Select(c => c.DisplayIndex).DefaultIfEmpty(0).Min();
    internal int LastVisibleDisplayIndex => this.Where(c => c.IsVisible).Select(c => c.DisplayIndex).DefaultIfEmpty(-1).Max();

    internal void InvalidateColumnRealization(bool invalidateAll) { }
    internal void InvalidateColumnWidthsComputation() { }
    internal void InvalidateAverageColumnWidth() { }
    internal void InvalidateHasVisibleStarColumns() { }
    internal void RedistributeColumnWidthsOnAvailableSpaceChange(double oldAvailable, double newAvailable) { }
    internal void RedistributeColumnWidthsOnWidthChangeOfColumn(DataGridColumn column, DataGridLength oldWidth) { }
    internal void RedistributeColumnWidthsOnMinWidthChangeOfColumn(DataGridColumn column, double oldMinWidth) { }
    internal void RedistributeColumnWidthsOnMaxWidthChangeOfColumn(DataGridColumn column, double oldMaxWidth) { }
    internal void RecomputeColumnWidthsOnColumnResize(DataGridColumn resizingColumn, double horizontalChange, bool retainAuto) { }
    internal bool RefreshAutoWidthColumns { get; set; }
}
