using System.Windows.Controls;
using System.Windows.Documents;
using System.Reflection;
using Xunit;

namespace LeXtudio.Windows.Tests;

public sealed class RichTextBoxDocumentsTests
{
    [Fact]
    public void RichTextBoxExposesWpfDocumentSurface()
    {
        var documentProperty = typeof(RichTextBox).GetProperty(nameof(RichTextBox.Document));

        Assert.NotNull(documentProperty);
        Assert.Equal(typeof(FlowDocument), documentProperty!.PropertyType);
        Assert.True(documentProperty.CanRead);
        Assert.True(documentProperty.CanWrite);
    }

    [Fact]
    public void TextBoxBaseAppendTextIsAvailableToRichTextBox()
    {
        var appendText = typeof(RichTextBox).GetMethod(
            nameof(RichTextBox.AppendText),
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            types: [typeof(string)],
            modifiers: null);

        Assert.NotNull(appendText);
        Assert.Equal(typeof(System.Windows.Controls.Primitives.TextBoxBase), appendText!.DeclaringType);
    }

    [Fact]
    public void FlowDocumentExposesTextRangeBoundaries()
    {
        var contentStart = typeof(FlowDocument).GetProperty(nameof(FlowDocument.ContentStart));
        var contentEnd = typeof(FlowDocument).GetProperty(nameof(FlowDocument.ContentEnd));
        var blocks = typeof(FlowDocument).GetProperty(nameof(FlowDocument.Blocks));

        Assert.NotNull(contentStart);
        Assert.Equal(typeof(TextPointer), contentStart!.PropertyType);
        Assert.NotNull(contentEnd);
        Assert.Equal(typeof(TextPointer), contentEnd!.PropertyType);
        Assert.NotNull(blocks);
        Assert.Equal(typeof(BlockCollection), blocks!.PropertyType);
    }

    [Fact]
    public void TextRangeHasPublicPointerPairConstructorAndTextProperty()
    {
        var constructor = typeof(TextRange).GetConstructor([typeof(TextPointer), typeof(TextPointer)]);
        var textProperty = typeof(TextRange).GetProperty(nameof(TextRange.Text));

        Assert.NotNull(constructor);
        Assert.NotNull(textProperty);
        Assert.Equal(typeof(string), textProperty!.PropertyType);
        Assert.True(textProperty.CanRead);
        Assert.True(textProperty.CanWrite);
    }
}
