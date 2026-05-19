namespace System.Windows.Documents;

// Early-batch placeholders for table and highlight subsystems while upstream
// TextRange/TextSelection are being enabled incrementally.
public class Table : Block, MS.Internal.Documents.IAcceptInsertion
{
    private readonly TableRowGroupCollection _rowGroups;

    public Table()
    {
        _rowGroups = new TableRowGroupCollection();
        _rowGroups.Add(new TableRowGroup());
        Columns = new TableColumnCollection(this);
    }

    public TableRowGroupCollection RowGroups => _rowGroups;

    public TableColumnCollection Columns { get; }

    internal void InvalidateColumns() { }

    int MS.Internal.Documents.IAcceptInsertion.InsertionIndex { get; set; }
}

public sealed class TableRowGroupCollection : List<TableRowGroup>
{
}

public class TableRowGroup : TextElement
{
    private readonly TableRowCollection _rows = new();

    public TableRowCollection Rows => _rows;
}

public sealed class TableRowCollection : List<TableRow>
{
}

public class TableRow : TextElement
{
    private readonly TableCellCollection _cells = new();

    public TableRowGroup? RowGroup { get; set; }

    public int Index { get; set; }

    public TableCellCollection Cells => _cells;
}

public class TableCell : TextElement
{
    public TableRow? Row { get; set; }

    public int RowSpan { get; set; } = 1;

    public int ColumnSpan { get; set; } = 1;

    public int ColumnIndex { get; set; }
}

public sealed class TableCellCollection : List<TableCell>
{
}

internal sealed class HighlightsCollection
{
    private readonly Dictionary<Type, HighlightLayer> _layers = [];

    internal void AddLayer(HighlightLayer layer)
    {
        if (layer is null)
        {
            return;
        }

        _layers[layer.GetType()] = layer;
    }

    internal void RemoveLayer(HighlightLayer layer)
    {
        if (layer is null)
        {
            return;
        }

        _layers.Remove(layer.GetType());
    }

    internal HighlightLayer? GetLayer(Type ownerType)
    {
        if (ownerType == typeof(TextSelection) && _layers.TryGetValue(typeof(TextSelectionHighlightLayer), out var textSelectionLayer))
        {
            return textSelectionLayer;
        }

        _layers.TryGetValue(ownerType, out var layer);
        return layer;
    }
}
