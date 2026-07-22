using System.Windows.Controls;
using System.Windows.Documents;
using System.Reflection;
using NUnit.Framework;

namespace LeXtudio.Windows.Tests;

[TestFixture]
public sealed class RichTextBoxDocumentsTests
{
    [Test]
    public void RichTextBoxExposesWpfDocumentSurface()
    {
        var documentProperty = typeof(RichTextBox).GetProperty(nameof(RichTextBox.Document));

        Assert.That(documentProperty, Is.Not.Null);
        Assert.That(documentProperty!.PropertyType, Is.EqualTo(typeof(FlowDocument)));
        Assert.That(documentProperty.CanRead, Is.True);
        Assert.That(documentProperty.CanWrite, Is.True);
    }

    [Test]
    public void TextBoxBaseAppendTextIsAvailableToRichTextBox()
    {
        var appendText = typeof(RichTextBox).GetMethod(
            nameof(RichTextBox.AppendText),
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            types: [typeof(string)],
            modifiers: null);

        Assert.That(appendText, Is.Not.Null);
        Assert.That(appendText!.DeclaringType, Is.EqualTo(typeof(System.Windows.Controls.Primitives.TextBoxBase)));
    }

    [Test]
    public void FlowDocumentExposesTextRangeBoundaries()
    {
        var contentStart = typeof(FlowDocument).GetProperty(nameof(FlowDocument.ContentStart));
        var contentEnd = typeof(FlowDocument).GetProperty(nameof(FlowDocument.ContentEnd));
        var blocks = typeof(FlowDocument).GetProperty(nameof(FlowDocument.Blocks));

        Assert.That(contentStart, Is.Not.Null);
        Assert.That(contentStart!.PropertyType, Is.EqualTo(typeof(TextPointer)));
        Assert.That(contentEnd, Is.Not.Null);
        Assert.That(contentEnd!.PropertyType, Is.EqualTo(typeof(TextPointer)));
        Assert.That(blocks, Is.Not.Null);
        Assert.That(blocks!.PropertyType, Is.EqualTo(typeof(BlockCollection)));
    }

    [Test]
    public void TextRangeHasPublicPointerPairConstructorAndTextProperty()
    {
        var constructor = typeof(TextRange).GetConstructor([typeof(TextPointer), typeof(TextPointer)]);
        var textProperty = typeof(TextRange).GetProperty(nameof(TextRange.Text));

        Assert.That(constructor, Is.Not.Null);
        Assert.That(textProperty, Is.Not.Null);
        Assert.That(textProperty!.PropertyType, Is.EqualTo(typeof(string)));
        Assert.That(textProperty.CanRead, Is.True);
        Assert.That(textProperty.CanWrite, Is.True);
    }
}
