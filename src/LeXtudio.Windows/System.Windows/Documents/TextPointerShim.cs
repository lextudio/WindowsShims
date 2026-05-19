using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Windows.Documents;

public static class TextPointerShim
{
    private sealed class State(DependencyObject parent, int offset)
    {
        public DependencyObject Parent { get; } = parent;
        public int Offset { get; } = offset;
    }

    private static readonly ConditionalWeakTable<TextPointer, State> s_states = new();

    public static TextPointer Create(DependencyObject parent, int offset)
    {
        ArgumentNullException.ThrowIfNull(parent);

#pragma warning disable SYSLIB0050
        var pointer = (TextPointer)FormatterServices.GetUninitializedObject(typeof(TextPointer));
#pragma warning restore SYSLIB0050

        s_states.Add(pointer, new State(parent, offset));
        return pointer;
    }

    public static DependencyObject GetParent(TextPointer pointer)
    {
        ArgumentNullException.ThrowIfNull(pointer);

        return s_states.TryGetValue(pointer, out var state) ? state.Parent : pointer.Parent;
    }

    public static int GetOffset(TextPointer pointer)
    {
        ArgumentNullException.ThrowIfNull(pointer);

        return s_states.TryGetValue(pointer, out var state) ? state.Offset : 0;
    }
}
