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
    private FontWeight _typingFontWeight = FontWeights.Normal;
    private FontStyle _typingFontStyle = FontStyles.Normal;

    public RichTextBox()
        : this(null)
    {
    }

    public RichTextBox(FlowDocument? document)
    {
        _implicitDocument = document is null;
        Document = document ?? CreateImplicitDocument();
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
        if (string.IsNullOrEmpty(textData))
        {
            return;
        }

        var paragraph = EnsureLastParagraph();
        var run = new Run(textData)
        {
            FontWeight = _typingFontWeight,
            FontStyle = _typingFontStyle,
        };

        paragraph.Inlines.Add(run);

        if (Selection is not null)
        {
            Selection.Select(run.ContentEnd, run.ContentEnd);
        }
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

    private Paragraph EnsureLastParagraph()
    {
        if (Document.Blocks.LastBlock is Paragraph paragraph)
        {
            return paragraph;
        }

        var appended = new Paragraph();
        Document.Blocks.Add(appended);
        return appended;
    }

    internal void ApplyTypingProperty(DependencyProperty property, object? value)
    {
        if (property == TextElement.FontWeightProperty && TryGetFontWeight(value, out var fontWeight))
        {
            _typingFontWeight = fontWeight;
            return;
        }

        if (property == TextElement.FontStyleProperty && TryGetFontStyle(value, out var fontStyle))
        {
            _typingFontStyle = fontStyle;
        }
    }

    private static bool TryGetFontWeight(object? value, out FontWeight fontWeight)
    {
        switch (value)
        {
            case FontWeight direct:
                fontWeight = direct;
                return true;
            case string text when string.Equals(text, nameof(FontWeights.Bold), StringComparison.OrdinalIgnoreCase):
                fontWeight = FontWeights.Bold;
                return true;
            case string text when string.Equals(text, nameof(FontWeights.Normal), StringComparison.OrdinalIgnoreCase):
                fontWeight = FontWeights.Normal;
                return true;
            default:
                fontWeight = FontWeights.Normal;
                return false;
        }
    }

    private static bool TryGetFontStyle(object? value, out FontStyle fontStyle)
    {
        switch (value)
        {
            case FontStyle direct:
                fontStyle = direct;
                return true;
            case string text when string.Equals(text, nameof(FontStyles.Italic), StringComparison.OrdinalIgnoreCase):
                fontStyle = FontStyles.Italic;
                return true;
            case string text when string.Equals(text, nameof(FontStyles.Oblique), StringComparison.OrdinalIgnoreCase):
                fontStyle = FontStyles.Oblique;
                return true;
            case string text when string.Equals(text, nameof(FontStyles.Normal), StringComparison.OrdinalIgnoreCase):
                fontStyle = FontStyles.Normal;
                return true;
            default:
                fontStyle = FontStyles.Normal;
                return false;
        }
    }
}

public sealed class TextEditorShim
{
    internal MS.Internal.Documents.UndoManager? _GetUndoManager() => null;
}
