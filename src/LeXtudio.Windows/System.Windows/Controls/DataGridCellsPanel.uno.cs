using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using MS.Internal;

namespace System.Windows.Controls;

public partial class DataGridCellsPanel
{
    private DataGrid? _parentDataGrid;
    private List<UIElement>? _realizedChildren;
    private UIElement? _clippedChildForFrozenBehaviour;
    private readonly RectangleGeometry _childClipForFrozenBehavior = new();

    internal bool HasCorrectRealizedColumns
    {
        get
        {
            var parentDataGrid = ParentDataGrid;
            if (parentDataGrid is null)
            {
                return true;
            }

            var columns = (DataGridColumnCollection)parentDataGrid.Columns;
            EnsureRealizedChildren();
            var children = RealizedChildren;

            if (children.Count == columns.Count)
            {
                return true;
            }

            var displayIndexMap = columns.DisplayIndexMap;
            var blockList = RealizedColumnsBlockList;
            if (blockList is null || blockList.Count == 0)
            {
                return true;
            }

            var k = 0;
            var n = children.Count;
            for (var j = 0; j < blockList.Count; ++j)
            {
                var block = blockList[j];
                for (var index = block.StartIndex; index <= block.EndIndex; ++index)
                {
                    for (; k < n; ++k)
                    {
                        if (children[k] is IProvideDataGridColumn cell)
                        {
                            var displayIndex = cell.Column.DisplayIndex;
                            var childColumnIndex = displayIndex < 0 ? -1 : displayIndexMap[displayIndex];
                            if (index < childColumnIndex)
                            {
                                return false;
                            }

                            if (index == childColumnIndex)
                            {
                                break;
                            }
                        }
                    }

                    if (k == n)
                    {
                        return false;
                    }

                    ++k;
                }
            }

            return true;
        }
    }

    protected override void OnIsItemsHostChanged(bool oldIsItemsHost, bool newIsItemsHost)
    {
        base.OnIsItemsHostChanged(oldIsItemsHost, newIsItemsHost);

        var parentPresenter = ParentPresenter;
        if (parentPresenter is null)
        {
            return;
        }

        if (newIsItemsHost)
        {
            if (parentPresenter.ItemContainerGenerator is IItemContainerGenerator generator &&
                ReferenceEquals(generator, generator.GetItemContainerGeneratorForPanel(this)))
            {
                if (parentPresenter is DataGridCellsPresenter cellsPresenter)
                {
                    cellsPresenter.InternalItemsHost = this;
                }
                else if (parentPresenter is DataGridColumnHeadersPresenter headersPresenter)
                {
                    headersPresenter.InternalItemsHost = this;
                }
            }
        }
        else
        {
            if (parentPresenter is DataGridCellsPresenter cellsPresenter)
            {
                if (ReferenceEquals(cellsPresenter.InternalItemsHost, this))
                {
                    cellsPresenter.InternalItemsHost = null;
                }
            }
            else if (parentPresenter is DataGridColumnHeadersPresenter headersPresenter &&
                     ReferenceEquals(headersPresenter.InternalItemsHost, this))
            {
                headersPresenter.InternalItemsHost = null;
            }
        }
    }

    protected override void OnItemsChanged(object sender, ItemsChangedEventArgs args)
    {
        base.OnItemsChanged(sender, args);

        switch (args.Action)
        {
            case NotifyCollectionChangedAction.Remove:
                OnItemsRemove(args);
                break;
            case NotifyCollectionChangedAction.Replace:
                OnItemsReplace(args);
                break;
            case NotifyCollectionChangedAction.Move:
                OnItemsMove(args);
                break;
        }
    }

    protected override void OnClearChildren()
    {
        base.OnClearChildren();
        _realizedChildren = null;
    }

    private void DetermineVirtualizationState()
    {
        var parentPresenter = ParentPresenter;
        if (parentPresenter is not null)
        {
            IsVirtualizing = VirtualizingPanel.GetIsVirtualizing(parentPresenter);
            InRecyclingMode = VirtualizingPanel.GetVirtualizationMode(parentPresenter) == VirtualizationMode.Recycling;
        }
    }

    private IList RealizedChildren
        => IsVirtualizing && InRecyclingMode
            ? (_realizedChildren is not null ? _realizedChildren : Array.Empty<UIElement>())
            : InternalChildren;

    private void EnsureRealizedChildren()
    {
        if (IsVirtualizing && InRecyclingMode)
        {
            if (_realizedChildren is null)
            {
                var children = InternalChildren;
                _realizedChildren = new List<UIElement>(children.Count);
                for (var i = 0; i < children.Count; i++)
                {
                    _realizedChildren.Add(children[i]);
                }
            }
        }
        else
        {
            _realizedChildren = null;
        }
    }

    private void OnItemsRemove(ItemsChangedEventArgs args)
        => RemoveChildRange(args.Position, args.ItemCount, args.ItemUICount);

    private void OnItemsReplace(ItemsChangedEventArgs args)
        => RemoveChildRange(args.Position, args.ItemCount, args.ItemUICount);

    private void OnItemsMove(ItemsChangedEventArgs args)
        => RemoveChildRange(args.OldPosition, args.ItemCount, args.ItemUICount);

    private void RemoveChildRange(GeneratorPosition position, int itemCount, int itemUICount)
    {
        if (!IsItemsHost)
        {
            return;
        }

        var children = InternalChildren;
        var pos = position.Index;
        if (position.Offset > 0)
        {
            pos++;
        }

        if (pos >= children.Count)
        {
            return;
        }

        if (itemUICount > 0)
        {
            RemoveInternalChildRange(pos, itemUICount);
            if (IsVirtualizing && InRecyclingMode && _realizedChildren is not null)
            {
                _realizedChildren.RemoveRange(pos, Math.Min(itemUICount, _realizedChildren.Count - pos));
            }
        }
    }

    private List<RealizedColumnsBlock>? RealizedColumnsBlockList
    {
        get
        {
            var dataGrid = ParentDataGrid;
            if (dataGrid is null)
            {
                return null;
            }

            var columns = dataGrid.InternalColumns;
            return IsVirtualizing
                ? columns.RealizedColumnsBlockListForVirtualizedRows
                : columns.RealizedColumnsBlockListForNonVirtualizedRows;
        }

        set
        {
            var dataGrid = ParentDataGrid;
            if (dataGrid is null)
            {
                return;
            }

            if (IsVirtualizing)
            {
                dataGrid.InternalColumns.RealizedColumnsBlockListForVirtualizedRows = value;
            }
            else
            {
                dataGrid.InternalColumns.RealizedColumnsBlockListForNonVirtualizedRows = value;
            }
        }
    }

    private List<RealizedColumnsBlock>? RealizedColumnsDisplayIndexBlockList
    {
        get
        {
            var dataGrid = ParentDataGrid;
            if (dataGrid is null)
            {
                return null;
            }

            var columns = dataGrid.InternalColumns;
            return IsVirtualizing
                ? columns.RealizedColumnsDisplayIndexBlockListForVirtualizedRows
                : columns.RealizedColumnsDisplayIndexBlockListForNonVirtualizedRows;
        }

        set
        {
            var dataGrid = ParentDataGrid;
            if (dataGrid is null)
            {
                return;
            }

            if (IsVirtualizing)
            {
                dataGrid.InternalColumns.RealizedColumnsDisplayIndexBlockListForVirtualizedRows = value;
            }
            else
            {
                dataGrid.InternalColumns.RealizedColumnsDisplayIndexBlockListForNonVirtualizedRows = value;
            }
        }
    }

    private bool RebuildRealizedColumnsBlockList
    {
        get
        {
            var dataGrid = ParentDataGrid;
            if (dataGrid is null)
            {
                return true;
            }

            var columns = dataGrid.InternalColumns;
            return IsVirtualizing
                ? columns.RebuildRealizedColumnsBlockListForVirtualizedRows
                : columns.RebuildRealizedColumnsBlockListForNonVirtualizedRows;
        }

        set
        {
            var dataGrid = ParentDataGrid;
            if (dataGrid is null)
            {
                return;
            }

            if (IsVirtualizing)
            {
                dataGrid.InternalColumns.RebuildRealizedColumnsBlockListForVirtualizedRows = value;
            }
            else
            {
                dataGrid.InternalColumns.RebuildRealizedColumnsBlockListForNonVirtualizedRows = value;
            }
        }
    }

    private void UpdateRealizedBlockLists(
        List<int> realizedColumnIndices,
        List<int> realizedColumnDisplayIndices,
        bool redeterminationNeeded)
    {
        realizedColumnIndices.Sort();
        RealizedColumnsBlockList = BuildRealizedColumnsBlockList(realizedColumnIndices);
        RealizedColumnsDisplayIndexBlockList = BuildRealizedColumnsBlockList(realizedColumnDisplayIndices);

        if (!redeterminationNeeded)
        {
            RebuildRealizedColumnsBlockList = false;
        }
    }

    private static List<RealizedColumnsBlock> BuildRealizedColumnsBlockList(List<int> indexList)
    {
        var resultList = new List<RealizedColumnsBlock>();
        if (indexList.Count == 1)
        {
            resultList.Add(new RealizedColumnsBlock(indexList[0], indexList[0], 0));
        }
        else if (indexList.Count > 0)
        {
            var startIndex = indexList[0];
            for (var i = 1; i < indexList.Count; i++)
            {
                if (indexList[i] != indexList[i - 1] + 1)
                {
                    if (resultList.Count == 0)
                    {
                        resultList.Add(new RealizedColumnsBlock(startIndex, indexList[i - 1], 0));
                    }
                    else
                    {
                        var lastBlock = resultList[^1];
                        var startIndexOffset = lastBlock.StartIndexOffset + lastBlock.EndIndex - lastBlock.StartIndex + 1;
                        resultList.Add(new RealizedColumnsBlock(startIndex, indexList[i - 1], startIndexOffset));
                    }

                    startIndex = indexList[i];
                }

                if (i == indexList.Count - 1)
                {
                    if (resultList.Count == 0)
                    {
                        resultList.Add(new RealizedColumnsBlock(startIndex, indexList[i], 0));
                    }
                    else
                    {
                        var lastBlock = resultList[^1];
                        var startIndexOffset = lastBlock.StartIndexOffset + lastBlock.EndIndex - lastBlock.StartIndex + 1;
                        resultList.Add(new RealizedColumnsBlock(startIndex, indexList[i], startIndexOffset));
                    }
                }
            }
        }

        return resultList;
    }

    internal double ComputeCellsPanelHorizontalOffset()
    {
        var dataGrid = ParentDataGrid;
        if (dataGrid is null)
        {
            return 0.0;
        }

        var horizontalOffset = dataGrid.HorizontalScrollOffset;
        var scrollViewer = dataGrid.InternalScrollHost;
        if (scrollViewer is null)
        {
            return 0.0;
        }

        return horizontalOffset + VisualTreeHelper.GetOffset(this).X;
    }

    internal Geometry? GetFrozenClipForChild(UIElement child)
    {
        if (ReferenceEquals(child, _clippedChildForFrozenBehaviour))
        {
            return _childClipForFrozenBehavior;
        }

        return null;
    }

    private static void MeasureChild(UIElement child, Size constraint)
    {
        var cell = child as IProvideDataGridColumn;
        var isColumnHeader = child is DataGridColumnHeader;
        var childMeasureConstraint = new Size(double.PositiveInfinity, constraint.Height);

        var desiredWidth = 0.0;
        var remeasure = false;

        if (cell is not null)
        {
            var column = cell.Column;
            var width = column.Width;
            if (width.IsAuto ||
                (width.IsSizeToHeader && isColumnHeader) ||
                (width.IsSizeToCells && !isColumnHeader))
            {
                child.Measure(childMeasureConstraint);
                desiredWidth = child.DesiredSize.Width;
                remeasure = true;
            }

            childMeasureConstraint.Width = column.GetConstraintWidth(isColumnHeader);
        }

        if (DoubleUtil.AreClose(desiredWidth, 0.0))
        {
            child.Measure(childMeasureConstraint);
        }

        var childDesiredSize = child.DesiredSize;
        if (cell is not null)
        {
            var column = cell.Column;
            column.UpdateDesiredWidthForAutoColumn(
                isColumnHeader,
                DoubleUtil.AreClose(desiredWidth, 0.0) ? childDesiredSize.Width : desiredWidth);

            var width = column.Width;
            if (remeasure &&
                !double.IsNaN(width.DisplayValue) &&
                DoubleUtil.GreaterThan(desiredWidth, width.DisplayValue))
            {
                childMeasureConstraint.Width = width.DisplayValue;
                child.Measure(childMeasureConstraint);
            }
        }
    }

    private static GeneratorPosition IndexToGeneratorPositionForStart(IItemContainerGenerator generator, int index, out int childIndex)
    {
        var position = generator is not null
            ? generator.GeneratorPositionFromIndex(index)
            : new GeneratorPosition(-1, index + 1);

        childIndex = position.Offset == 0 ? position.Index : position.Index + 1;
        return position;
    }

    private UIElement? GenerateChild(
        IItemContainerGenerator generator,
        Size constraint,
        DataGridColumn column,
        ref IDisposable? generatorState,
        ref int childIndex,
        out Size childSize)
    {
        if (generatorState is null)
        {
            generatorState = generator.StartAt(
                IndexToGeneratorPositionForStart(generator, childIndex, out childIndex),
                GeneratorDirection.Forward,
                true);
        }

        return GenerateChild(generator, constraint, column, ref childIndex, out childSize);
    }

    private UIElement? GenerateChild(
        IItemContainerGenerator generator,
        Size constraint,
        DataGridColumn column,
        ref int childIndex,
        out Size childSize)
    {
        var child = generator.GenerateNext(out var newlyRealized) as UIElement;
        if (child is null)
        {
            childSize = new Size();
            return null;
        }

        AddContainerFromGenerator(childIndex, child, newlyRealized);
        childIndex++;

        MeasureChild(child, constraint);

        var width = column.Width;
        childSize = child.DesiredSize;
        if (!double.IsNaN(width.DisplayValue))
        {
            childSize = new Size(width.DisplayValue, childSize.Height);
        }

        return child;
    }

    private Size GenerateChildren(
        IItemContainerGenerator generator,
        int startIndex,
        int endIndex,
        Size constraint)
    {
        var measureWidth = 0.0;
        var measureHeight = 0.0;
        var startPos = IndexToGeneratorPositionForStart(generator, startIndex, out var childIndex);
        var parentDataGrid = ParentDataGrid;
        if (parentDataGrid is null)
        {
            return new Size(measureWidth, measureHeight);
        }

        using (generator.StartAt(startPos, GeneratorDirection.Forward, true))
        {
            for (var i = startIndex; i <= endIndex; i++)
            {
                if (!parentDataGrid.Columns[i].IsVisible)
                {
                    continue;
                }

                if (GenerateChild(generator, constraint, parentDataGrid.Columns[i], ref childIndex, out var childSize) is null)
                {
                    return new Size(measureWidth, measureHeight);
                }

                measureWidth += childSize.Width;
                measureHeight = Math.Max(measureHeight, childSize.Height);
            }
        }

        return new Size(measureWidth, measureHeight);
    }

    private void AddContainerFromGenerator(int childIndex, UIElement child, bool newlyRealized)
    {
        if (!newlyRealized)
        {
            if (InRecyclingMode)
            {
                var children = RealizedChildren;
                if (childIndex >= children.Count || !ReferenceEquals(children[childIndex], child))
                {
                    InsertRecycledContainer(childIndex, child);
                    child.Measure(new Size());
                }
            }

            return;
        }

        InsertNewContainer(childIndex, child);
    }

    private void InsertRecycledContainer(int childIndex, UIElement container)
        => InsertContainer(childIndex, container, true);

    private void InsertNewContainer(int childIndex, UIElement container)
        => InsertContainer(childIndex, container, false);

    private void InsertContainer(int childIndex, UIElement container, bool isRecycled)
    {
        var children = InternalChildren;
        var visualTreeIndex = 0;

        if (childIndex > 0)
        {
            visualTreeIndex = ChildIndexFromRealizedIndex(childIndex - 1) + 1;
        }

        if (isRecycled && visualTreeIndex < children.Count && ReferenceEquals(children[visualTreeIndex], container))
        {
            return;
        }

        if (visualTreeIndex < children.Count)
        {
            var insertIndex = visualTreeIndex;
            if (isRecycled && VisualTreeHelper.GetParent(container) is not null)
            {
                var containerIndex = children.IndexOf(container);
                RemoveInternalChildRange(containerIndex, 1);
                if (containerIndex < insertIndex)
                {
                    insertIndex--;
                }

                InsertInternalChild(insertIndex, container);
            }
            else
            {
                InsertInternalChild(insertIndex, container);
            }
        }
        else
        {
            if (isRecycled && VisualTreeHelper.GetParent(container) is not null)
            {
                var originalIndex = children.IndexOf(container);
                RemoveInternalChildRange(originalIndex, 1);
                AddInternalChild(container);
            }
            else
            {
                AddInternalChild(container);
            }
        }

        if (IsVirtualizing && InRecyclingMode)
        {
            _realizedChildren ??= new List<UIElement>();
            _realizedChildren.Insert(childIndex, container);
        }

        ParentPresenter?.ItemContainerGenerator.PrepareItemContainer(container);
    }

    private int ChildIndexFromRealizedIndex(int realizedChildIndex)
    {
        if (IsVirtualizing && InRecyclingMode && _realizedChildren is not null && realizedChildIndex < _realizedChildren.Count)
        {
            var child = _realizedChildren[realizedChildIndex];
            var children = InternalChildren;
            for (var i = realizedChildIndex; i < children.Count; i++)
            {
                if (ReferenceEquals(children[i], child))
                {
                    return i;
                }
            }
        }

        return realizedChildIndex;
    }

    private static double GetColumnEstimatedMeasureWidth(DataGridColumn column, double averageColumnWidth)
    {
        if (!column.IsVisible)
        {
            return 0.0;
        }

        var childMeasureWidth = column.Width.DisplayValue;
        if (double.IsNaN(childMeasureWidth))
        {
            childMeasureWidth = Math.Max(averageColumnWidth, column.MinWidth);
            childMeasureWidth = Math.Min(childMeasureWidth, column.MaxWidth);
        }

        return childMeasureWidth;
    }

    private double GetColumnEstimatedMeasureWidthSum(int startIndex, int endIndex, double averageColumnWidth)
    {
        var measureWidth = 0.0;
        var parentDataGrid = ParentDataGrid;
        if (parentDataGrid is null)
        {
            return measureWidth;
        }

        for (var i = startIndex; i <= endIndex; i++)
        {
            measureWidth += GetColumnEstimatedMeasureWidth(parentDataGrid.Columns[i], averageColumnWidth);
        }

        return measureWidth;
    }

    private double GetViewportWidth()
    {
        var availableViewportWidth = 0.0;
        var parentDataGrid = ParentDataGrid;
        if (parentDataGrid is not null)
        {
            var scrollContentPresenter = parentDataGrid.InternalScrollContentPresenter;
            if (scrollContentPresenter is not null && !scrollContentPresenter.CanContentScroll)
            {
                availableViewportWidth = scrollContentPresenter.ActualWidth;
            }
            else if (parentDataGrid.InternalItemsHost is IScrollInfo scrollInfo)
            {
                availableViewportWidth = scrollInfo.ViewportWidth;
            }
        }

        var parentRowsPresenter = ParentRowsPresenter;
        if (Math.Abs(availableViewportWidth) < double.Epsilon && parentRowsPresenter is not null)
        {
            var rowPresenterAvailableSize = parentRowsPresenter.AvailableSize;
            if (!double.IsNaN(rowPresenterAvailableSize.Width) && !double.IsInfinity(rowPresenterAvailableSize.Width))
            {
                availableViewportWidth = rowPresenterAvailableSize.Width;
            }
        }

        return availableViewportWidth;
    }

    private DataGridRowsPresenter? ParentRowsPresenter
    {
        get
        {
            var parentDataGrid = ParentDataGrid;
            if (parentDataGrid is null)
            {
                return null;
            }

            if (!parentDataGrid.IsGrouping)
            {
                return parentDataGrid.InternalItemsHost as DataGridRowsPresenter;
            }

            if (ParentPresenter is DataGridCellsPresenter presenter)
            {
                var row = presenter.DataGridRowOwner;
                if (row is not null)
                {
                    return VisualTreeHelper.GetParent(row) as DataGridRowsPresenter;
                }
            }

            return null;
        }
    }

    private ObservableCollection<DataGridColumn>? Columns => ParentDataGrid?.Columns;

    private DataGrid? ParentDataGrid
    {
        get
        {
            if (_parentDataGrid is not null)
            {
                return _parentDataGrid;
            }

            if (ParentPresenter is DataGridCellsPresenter presenter)
            {
                _parentDataGrid = presenter.DataGridRowOwner?.DataGridOwner;
            }
            else if (ParentPresenter is DataGridColumnHeadersPresenter headersPresenter)
            {
                _parentDataGrid = headersPresenter.ParentDataGrid;
            }

            return _parentDataGrid;
        }
    }

    private ItemsControl? ParentPresenter
    {
        get
        {
            if (TemplatedParent is FrameworkElement itemsPresenter)
            {
                return itemsPresenter.TemplatedParent as ItemsControl;
            }

            return null;
        }
    }
}
