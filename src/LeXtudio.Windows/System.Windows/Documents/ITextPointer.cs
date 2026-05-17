namespace System.Windows.Documents;

public interface ITextPointer
{
    int CompareTo(ITextPointer position);
    TextContainer TextContainer { get; }
    LogicalDirection LogicalDirection { get; }
    bool IsFrozen { get; }
    bool HasValidLayout { get; }
    TextPointerContext GetPointerContext(LogicalDirection direction);
    ITextPointer CreatePointer();
    ITextPointer CreatePointer(int distance);
    ITextPointer CreatePointer(LogicalDirection gravity);
    ITextPointer GetFrozenPointer(LogicalDirection logicalDirection);
    ITextPointer? GetNextContextPosition(LogicalDirection direction);
    ITextPointer? GetNextInsertionPosition(LogicalDirection direction);
    ITextPointer GetInsertionPosition(LogicalDirection direction);
    bool MoveToInsertionPosition(LogicalDirection direction);
    bool MoveToNextInsertionPosition(LogicalDirection direction);
    void MoveToNextContextPosition(LogicalDirection direction);
    void SetLogicalDirection(LogicalDirection direction);
    string GetTextInRun(LogicalDirection direction);
    int GetTextInRun(LogicalDirection direction, char[] textBuffer, int startIndex, int count);
    Type? GetElementType(LogicalDirection direction);
    void Freeze();
    bool ValidateLayout();
    System.Type? ParentType { get; }
    int Offset { get; }
}
