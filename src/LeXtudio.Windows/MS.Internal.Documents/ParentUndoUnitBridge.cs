#if WINUI_BRIDGE
// Bridge file: defines UndoManager in MS.Internal.Documents namespace.
// WPF places UndoManager here; our earlier shim had it in System.Windows.Documents.

namespace MS.Internal.Documents
{
    internal sealed class UndoManager
    {
        internal bool IsEnabled { get; set; }
        internal int UndoCount => 0;
        internal int RedoCount => 0;
        internal int MinUndoStackCount => 0;
        internal IParentUndoUnit? OpenedUnit => null;

        internal void Clear() { }
        internal void Add(object undoUnit) { }
        internal void Open(IParentUndoUnit parentUndoUnit) { }
        internal void Close(IParentUndoUnit parentUndoUnit, UndoCloseAction closeAction) { }
        internal void OnNextDiscard() { }
    }
}
#endif
