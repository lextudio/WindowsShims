using System.Collections.Specialized;

namespace System.Windows.Controls;

// Split out of the former local RealizedColumnsBlock.cs (that struct itself is now linked
// from upstream WPF — see LeXtudio.Windows.csproj). Real WPF puts these two types in
// System.Windows.Controls.Primitives; kept in the root System.Windows.Controls namespace
// here (as the original file had them) to avoid a namespace migration across every
// consumer that references them unqualified.
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
