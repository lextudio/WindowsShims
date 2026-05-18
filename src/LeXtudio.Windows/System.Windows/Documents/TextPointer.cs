namespace System.Windows.Documents;

public sealed class TextSelectionShim
{
    public TextEditorShim TextEditor { get; } = new();
}

public sealed class TextEditorShim
{
    public bool IsReadOnly { get; set; }
    public object? _cursor { get; set; }
}
