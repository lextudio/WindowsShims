namespace System.Windows.Documents;

// Minimal stub. Upstream TextRangeEditLists.cs lives at
// ext/.../System/Windows/Documents/TextRangeEditLists.cs and is not yet
// promoted; only the entry points TextRangeEdit calls are stubbed here.
internal static class TextRangeEditLists
{
    internal static bool SplitListsForFlowDirectionChange(TextPointer start, TextPointer end, object? value) => true;

    internal static void MergeParagraphs(Block firstParagraph, Block secondParagraph)
    {
    }
}
