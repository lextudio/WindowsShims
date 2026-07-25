### Session 79 - TableCell Collection Population (OnNewParent)

Status: completed.

Scope:

- `TableCell`/`TableRow`/`TableRowGroup` override `OnNewParent` in the WPF linked
  source to register/unregister themselves with their parent's collection (`InternalAdd`/
  `InternalRemove`). The shim's `LogicalTreeHelper.AddLogicalChild` only set `Parent`
  as an auto-property — `OnNewParent` was never called.
- This meant collection `Items` arrays were never populated: `row.Cells.Count` was 0,
  `rowGroup.Rows.Count` was 0, and any WPF code that iterated collections
  (e.g., `TextRangeEditTables` border correction, row deletion, cell selection)
  would silently skip or crash.

Implementation:

- Changed `FrameworkContentElement.Parent` from an auto-property to a manual
  property with a backing field. The setter calls `OnNewParent(value)` before
  storing the value.
- Made the base `OnNewParent` in `FrameworkContentElement` update `_parent`,
  matching WPF's behavior where `base.OnNewParent(newParent)` sets `_parent`.
  Overrides (TableCell, TableRow, TableRowGroup) call `base.OnNewParent` after
  reading the old parent, so `this.Parent` returns the correct value at each stage.

The chain now works:
```
row.Cells.Add(cell)
  → TableTextElementCollectionInternal.Add(cell)
    → cell.RepositionWithContent(Owner.ContentEnd)
      → tree.InsertElementInternal(...)
        → LogicalTreeHelper.AddLogicalChild(parent, cell)
          → cell.Parent = parent          // setter → OnNewParent
            → cell.OnNewParent(parentRow)
              → parentRow.Cells.InternalAdd(cell)  // populates Items array
```

Tests:

- `Table_CollectionCounts_AreCorrectAfterConstruction`: create a 2×2 table,
  verify `table.RowGroups.Count == 1`, `rowGroup.Rows.Count == 2`,
  `row.Cells.Count == 2`.

Files modified:

- `src/.../FrameworkElement.cs` — `FrameworkContentElement.Parent` manual
  property with `OnNewParent` call; base `OnNewParent` sets `_parent`.
- `tests/.../MainPage.cs` — `table-collection-counts` probe.
- `tests/.../RichTextBoxIntegrationTests.cs` — `Table_CollectionCounts_AreCorrectAfterConstruction` test.

Result:

- 188/188 RichTextBox integration tests pass (1 new, 0 failures).
- `TableRowGroup.Rows`, `TableRow.Cells` collections now correctly track
  their children after `Add` operations.
