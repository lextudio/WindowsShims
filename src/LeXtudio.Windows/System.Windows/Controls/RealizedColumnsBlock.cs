using System.Collections.Specialized;

namespace System.Windows.Controls;

internal readonly struct RealizedColumnsBlock
{
    internal RealizedColumnsBlock(int startIndex, int endIndex, int startIndexOffset)
    {
        StartIndex = startIndex;
        EndIndex = endIndex;
        StartIndexOffset = startIndexOffset;
    }

    internal int StartIndex { get; }

    internal int EndIndex { get; }

    internal int StartIndexOffset { get; }
}

public readonly struct GeneratorPosition
{
    public GeneratorPosition(int index, int offset)
    {
        Index = index;
        Offset = offset;
    }

    public int Index { get; }

    public int Offset { get; }
}

public class ItemsChangedEventArgs : EventArgs
{
    public NotifyCollectionChangedAction Action { get; init; }

    public GeneratorPosition Position { get; init; }

    public GeneratorPosition OldPosition { get; init; }

    public int ItemCount { get; init; }

    public int ItemUICount { get; init; }
}
