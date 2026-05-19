namespace System.Windows.Documents;

// Table* stubs removed in Session 23 — upstream Table.cs, TableRow.cs, TableCell.cs,
// TableRowGroup.cs, and collection files promoted from ext/wpf.

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
