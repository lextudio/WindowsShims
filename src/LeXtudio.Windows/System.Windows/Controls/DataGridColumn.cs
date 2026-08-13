namespace System.Windows.Controls;

// Session 63: the WPF DataGridColumn body is linked upstream. This partial
// carries only Uno render/edit bridge helpers used by the local visual path.
public abstract partial class DataGridColumn
{
    public void SetValue(DependencyPropertyKey key, object? value)
        => SetValue(key.DependencyProperty, value);

    public void CoerceValue(DependencyProperty dp)
    {
        // Session 130 slice 8 (item 5): activate the WPF CoerceValueCallbacks
        // reachable from real call sites, smallest-blast-radius whitelist (same
        // policy as DataGrid/DataGridRow/DataGridColumnHeader):
        //   ActualWidth — OnCoerceActualWidth clamps to MinWidth/MaxWidth and
        //     honors absolute Width (upstream DataGridColumn.cs:484; triggered
        //     from OnWidthPropertyChanged :238 in the _processingWidthChange
        //     branch, i.e. after a width-set cycle completes).
        //   MaxWidth — OnCoerceMaxWidth caps star columns at _starMaxWidth
        //     (upstream :432; triggered from OnWidthPropertyChanged :245 when
        //     IsStar changes). Transfer lookup (GetCoercedTransferPropertyValue)
        //     is inert here because the shim's TransferProperty applies visuals
        //     directly and never enables property transfer.
        //   IsFrozen — OnCoerceIsFrozen mirrors DisplayIndex < FrozenColumnCount
        //     (upstream :1274; triggered from DataGridColumnCollection.InsertItem/
        //     SetItem :58/:84, receiver typed DataGridColumn, binds here).
        //   CanUserSort (DataGridTemplateColumn only) — forces false when
        //     SortMemberPath is empty (upstream DataGridTemplateColumn.cs:37;
        //     triggered from OnTemplateColumnSortMemberPathChanged :31). This
        //     callback is registered via OverrideMetadata (a project-wide no-op
        //     under the shim), so it is invoked directly — and only for the
        //     template column subtype; base DataGridColumn.CanUserSort coercion
        //     stays dormant (transfer triad, item 5 note).
        if (dp == ActualWidthProperty)
        {
            SetCoerced(ActualWidthPropertyKey, OnCoerceActualWidth(this, GetValue(dp)));
        }
        else if (dp == MaxWidthProperty)
        {
            SetCoerced(MaxWidthProperty, OnCoerceMaxWidth(this, ShimCoerceBaseValue(MaxWidthProperty, double.PositiveInfinity, ref _shimMaxWidthBase)));
        }
        else if (dp == IsFrozenProperty)
        {
            SetCoerced(IsFrozenPropertyKey, OnCoerceIsFrozen(this, GetValue(dp)));
        }
        else if (dp == CanUserSortProperty && this is DataGridTemplateColumn)
        {
            SetCoerced(CanUserSortProperty, DataGridTemplateColumn.OnCoerceTemplateColumnCanUserSort(this, GetValue(dp)));
        }
    }

    private void SetCoerced(DependencyProperty property, object? coerced)
    {
        var current = GetValue(property);
        if (!Equals(coerced, current))
        {
            SetValue(property, coerced);
        }
    }

    // Same base-value capture as DataGrid.ShimCoerceBaseValue (session 130
    // findings 4): WPF passes the pre-coercion base value into the callback;
    // this bridge has no current-value layer, so once a coercion writes the
    // capped value the next read-back sees the capped value instead of the
    // default. Capture the first value per property and reuse it.
    private object ShimCoerceBaseValue(DependencyProperty property, object fallback, ref object? captured)
    {
        if (captured != null)
        {
            return captured;
        }

        var local = ReadLocalValue(property);
        captured = local == Microsoft.UI.Xaml.DependencyProperty.UnsetValue ? fallback : local;
        return captured;
    }

    private object? _shimMaxWidthBase;

    private void SetCoerced(System.Windows.DependencyPropertyKey key, object? coerced)
    {
        var property = key.DependencyProperty;
        var current = GetValue(property);
        if (!Equals(coerced, current))
        {
            SetValue(key, coerced);
        }
    }

    internal FrameworkElement? BuildCellContent(DataGridCell cell, object dataItem)
        => GenerateElement(cell, dataItem);

    internal FrameworkElement? BuildEditingCellContent(DataGridCell cell, object dataItem)
        => GenerateEditingElement(cell, dataItem);

    // Session 65: propagate the shim's computed width back to the linked
    // column's ActualWidth DP so probes (EffectiveColumnWidth) and any
    // WPF-linked code that reads column.ActualWidth see the real value.
    internal void SetActualWidth(double width)
    {
        var key = ActualWidthPropertyKey;
        SetValue(key, width);
    }
}
