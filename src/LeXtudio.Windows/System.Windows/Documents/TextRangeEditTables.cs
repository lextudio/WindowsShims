namespace System.Windows.Documents;

internal static class TextRangeEditTables
{
    internal static TextPointer EnsureInsertionPosition(TextPointer pointer) => pointer;

    internal static Table InsertTable(TextPointer insertionPosition, int rowCount, int columnCount)
    {
        var table = new Table();
        var rowGroup = table.RowGroups[0];

        rowCount = Math.Max(1, rowCount);
        columnCount = Math.Max(1, columnCount);

        for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
        {
            var row = new TableRow
            {
                RowGroup = rowGroup,
                Index = rowIndex,
            };

            for (var columnIndex = 0; columnIndex < columnCount; columnIndex++)
            {
                var cell = new TableCell
                {
                    Row = row,
                    ColumnIndex = columnIndex,
                };
                row.Cells.Add(cell);
            }

            rowGroup.Rows.Add(row);
        }

        return table;
    }

    internal static TextRange InsertRows(TextRange range, int rowCount)
    {
        return range;
    }

    internal static ITextPointer? GetNextTableCellRangeInsertionPosition(ITextSelection selection, LogicalDirection direction)
        => ((ITextSelection)selection).MovingPosition;

    internal static ITextPointer? GetNextRowEndMovingPosition(ITextSelection selection, LogicalDirection direction)
        => ((ITextSelection)selection).MovingPosition;

    internal static bool MovingPositionCrossesCellBoundary(ITextSelection selection) => false;

    internal static ITextPointer? GetNextRowStartMovingPosition(ITextSelection selection, LogicalDirection direction)
        => ((ITextSelection)selection).MovingPosition;

    internal static bool IsTableCellRange(TextPointer anchorPosition, TextPointer movingPosition, bool includeCellAtMovingPosition, out TableCell anchorCell, out TableCell movingCell)
    {
        anchorCell = null!;
        movingCell = null!;
        return false;
    }
}
