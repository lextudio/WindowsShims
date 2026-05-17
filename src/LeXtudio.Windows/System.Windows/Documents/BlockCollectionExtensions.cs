namespace System.Windows.Documents;

internal static class BlockCollectionExtensions
{
    internal static void InsertAfter(this BlockCollection blocks, Block previousBlock, Block newBlock)
    {
        blocks.Add(newBlock);
    }
}
