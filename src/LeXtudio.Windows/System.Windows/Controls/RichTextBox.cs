using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Markup;
using System.ComponentModel;

namespace System.Windows.Controls;

[ContentProperty("Document")]
public class RichTextBox : TextBoxBase, IAddChild
{
    private FlowDocument _document;
    private bool _implicitDocument;

    public RichTextBox()
        : this(null)
    {
    }

    public RichTextBox(FlowDocument? document)
    {
        _implicitDocument = document is null;
        Document = document ?? CreateImplicitDocument();
        Selection = new TextSelection(Document.ContentStart, Document.ContentStart);
    }

    public FlowDocument Document
    {
        get => _document;
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            if (ReferenceEquals(value, _document))
            {
                return;
            }

            if (value.Owner is not null && !ReferenceEquals(value.Owner, this))
            {
                throw new ArgumentException(SR.RichTextBox_DocumentBelongsToAnotherRichTextBoxAlready);
            }

            if (_document is not null)
            {
                _document.Owner = null;
                _document.Parent = null;
            }

            if (_document is not null)
            {
                _implicitDocument = false;
            }

            _document = value;
            _document.Owner = this;
            _document.Parent = this;

            if (Selection is not null)
            {
                Selection.Select(_document.ContentStart, _document.ContentStart);
            }
        }
    }

    public TextEditorShim TextEditor { get; } = new();

    public virtual TextPointer? GetPositionFromPoint(Point point, bool snapToText)
        => snapToText ? Document.ContentEnd : null;

    void IAddChild.AddChild(object value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value is not FlowDocument document)
        {
            throw new ArgumentException("RichTextBox content must be a FlowDocument.", nameof(value));
        }

        if (!_implicitDocument)
        {
            throw new ArgumentException(SR.Format(SR.CanOnlyHaveOneChild, GetType(), value.GetType()), nameof(value));
        }

        Document = document;
        _implicitDocument = false;
    }

    void IAddChild.AddText(string text)
        => XamlSerializerUtil.ThrowIfNonWhiteSpaceInAddText(text, this);

    public override void AppendText(string textData)
    {
    }

    public override void SelectAll()
        => Selection?.Select(Document.ContentStart, Document.ContentEnd);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool ShouldSerializeDocument()
    {
        var firstBlock = Document.Blocks.FirstBlock;
        if (!_implicitDocument)
        {
            return true;
        }

        if (firstBlock is null)
        {
            return false;
        }

        if (!ReferenceEquals(firstBlock, Document.Blocks.LastBlock) || firstBlock is not Paragraph paragraph)
        {
            return true;
        }

        var firstInline = paragraph.Inlines.FirstInline;
        if (firstInline is null)
        {
            return false;
        }

        if (!ReferenceEquals(firstInline, paragraph.Inlines.LastInline) || firstInline is not Run run)
        {
            return true;
        }

        return !string.IsNullOrEmpty(run.Text);
    }

    private static FlowDocument CreateImplicitDocument()
    {
        var document = new FlowDocument();
        document.Blocks.Add(new Paragraph());
        return document;
    }
}

public sealed class TextEditorShim
{
    internal System.Windows.Documents.UndoManager? _GetUndoManager() => null;
}
