using System.Text.Json;
using Xunit;

namespace RichTextBox.IntegrationTests;

[Collection("RichTextBox app")]
public sealed class RichTextBoxIntegrationTests
{
    readonly RichTextBoxAppFixture _app;

    public RichTextBoxIntegrationTests(RichTextBoxAppFixture app) => _app = app;

    static bool HasRichTextBox(JsonElement state) => state.GetProperty("hasRichTextBox").GetBoolean();
    static bool HasDocument(JsonElement state) => state.GetProperty("hasDocument").GetBoolean();
    static int BlockCount(JsonElement state) => state.GetProperty("blockCount").GetInt32();
    static string Text(JsonElement state) => state.GetProperty("text").GetString() ?? "";
    static string SelectionText(JsonElement state) => state.GetProperty("selectionText").GetString() ?? "";
    static string? SelectionFontWeight(JsonElement state) => state.GetProperty("selectionFontWeight").GetString();
    static string? FirstInlineFontWeight(JsonElement state) => state.GetProperty("firstInlineFontWeight").GetString();
    static string? FirstInlineFontStyle(JsonElement state) => state.GetProperty("firstInlineFontStyle").GetString();
    static bool FirstInlineHasUnderline(JsonElement state) => state.GetProperty("firstInlineHasUnderline").GetBoolean();
    static string? RenderScopeType(JsonElement state) => state.GetProperty("renderScopeType").GetString();
    static string? TextViewType(JsonElement state) => state.GetProperty("textViewType").GetString();

    [Fact]
    public async Task State_ReturnsRichTextBoxSnapshot()
    {
        var state = await _app.InvokeAsync("richtextbox.probe.state");

        Assert.True(state.TryGetProperty("hasRichTextBox", out _), state.ToString());
        Assert.True(state.TryGetProperty("hasDocument", out _), state.ToString());
        Assert.True(state.TryGetProperty("blockCount", out _), state.ToString());
        Assert.True(state.TryGetProperty("text", out _), state.ToString());
        Assert.True(state.TryGetProperty("selectionText", out _), state.ToString());
        Assert.True(state.TryGetProperty("selectionFontWeight", out _), state.ToString());
        Assert.True(state.TryGetProperty("firstInlineType", out _), state.ToString());
        Assert.True(state.TryGetProperty("firstInlineFontWeight", out _), state.ToString());
        Assert.True(state.TryGetProperty("firstInlineFontStyle", out _), state.ToString());
        Assert.True(state.TryGetProperty("firstInlineHasUnderline", out _), state.ToString());
        Assert.True(state.TryGetProperty("renderScopeType", out _), state.ToString());
        Assert.True(state.TryGetProperty("textViewType", out _), state.ToString());
    }

    [Fact]
    public async Task CreatePlain_AppendsTextIntoDefaultDocument()
    {
        var state = await _app.InvokeAsync("richtextbox.probe.create-plain", "hello");
        var raw = state.ToString();

        Assert.True(HasRichTextBox(state), raw);
        Assert.True(HasDocument(state), raw);
        Assert.Contains("hello", Text(state));
    }

    [Fact]
    public async Task CreatePlain_AttachesFlowDocumentRenderScope()
    {
        var state = await _app.InvokeAsync("richtextbox.probe.create-plain", "hello");
        var raw = state.ToString();

        Assert.True(HasRichTextBox(state), raw);
        Assert.True(state.GetProperty("contentHostAvailable").GetBoolean(), raw);
        Assert.Equal("MS.Internal.Documents.FlowDocumentView", RenderScopeType(state));
        Assert.Equal("MS.Internal.Documents.UnoFlowDocumentTextView", TextViewType(state));
    }

    [Fact]
    public async Task Append_MutatesExistingDocument()
    {
        await _app.InvokeAsync("richtextbox.probe.create-plain", "hello");

        var state = await _app.InvokeAsync("richtextbox.probe.append", " world");
        var raw = state.ToString();

        Assert.True(HasRichTextBox(state), raw);
        Assert.True(HasDocument(state), raw);
        Assert.Contains("hello", Text(state));
        Assert.Contains("world", Text(state));
    }

    [Fact]
    public async Task TextInput_MutatesDocumentThroughEditorTypingPath()
    {
        await _app.InvokeAsync("richtextbox.probe.create-plain", "");

        var state = await _app.InvokeAsync("richtextbox.probe.text-input", "abc");
        var raw = state.ToString();

        Assert.True(HasRichTextBox(state), raw);
        Assert.True(HasDocument(state), raw);
        Assert.Contains("abc", Text(state));
    }

    [Fact]
    public async Task TextInputEvent_MutatesDocumentThroughOnTextInputPath()
    {
        await _app.InvokeAsync("richtextbox.probe.create-plain", "");

        var state = await _app.InvokeAsync("richtextbox.probe.text-input-event", "xyz");
        var raw = state.ToString();

        Assert.True(HasRichTextBox(state), raw);
        Assert.True(HasDocument(state), raw);
        Assert.Contains("xyz", Text(state));
    }

    [Fact]
    public async Task TextInputEvent_ReplacesSelectedText()
    {
        await _app.InvokeAsync("richtextbox.probe.create-plain", "old text");

        var state = await _app.InvokeAsync("richtextbox.probe.replace-selection-text-input-event", "new");
        var raw = state.ToString();
        var text = Text(state);

        Assert.True(HasRichTextBox(state), raw);
        Assert.True(HasDocument(state), raw);
        Assert.Contains("new", text);
        Assert.DoesNotContain("old text", text);
    }

    [Fact]
    public async Task BackspaceCommand_RemovesPreviousCharacter()
    {
        await _app.InvokeAsync("richtextbox.probe.create-plain", "");
        await _app.InvokeAsync("richtextbox.probe.text-input-event", "abc");

        var state = await _app.InvokeAsync("richtextbox.probe.backspace-command");
        var raw = state.ToString();
        var text = Text(state);

        Assert.True(HasRichTextBox(state), raw);
        Assert.True(HasDocument(state), raw);
        Assert.Contains("ab", text);
        Assert.DoesNotContain("abc", text);
    }

    [Fact]
    public async Task DeleteCommand_RemovesSelectedText()
    {
        await _app.InvokeAsync("richtextbox.probe.create-plain", "");
        await _app.InvokeAsync("richtextbox.probe.text-input-event", "abc");

        var state = await _app.InvokeAsync("richtextbox.probe.delete-selection-command");
        var raw = state.ToString();
        var text = Text(state);

        Assert.True(HasRichTextBox(state), raw);
        Assert.True(HasDocument(state), raw);
        Assert.DoesNotContain("abc", text);
    }

    [Fact]
    public async Task ToggleBoldCommand_AppliesBoldToSelectedText()
    {
        await _app.InvokeAsync("richtextbox.probe.create-plain", "bold me");

        var state = await _app.InvokeAsync("richtextbox.probe.toggle-bold-selection-command");
        var raw = state.ToString();

        Assert.True(HasRichTextBox(state), raw);
        Assert.True(HasDocument(state), raw);
        Assert.Contains("bold me", SelectionText(state));
        Assert.Equal("700", FirstInlineFontWeight(state));
    }

    [Fact]
    public async Task ToggleItalicCommand_AppliesItalicToSelectedText()
    {
        await _app.InvokeAsync("richtextbox.probe.create-plain", "italic me");

        var state = await _app.InvokeAsync("richtextbox.probe.toggle-italic-selection-command");
        var raw = state.ToString();

        Assert.True(HasRichTextBox(state), raw);
        Assert.True(HasDocument(state), raw);
        Assert.Contains("italic me", SelectionText(state));
        Assert.Equal("Italic", FirstInlineFontStyle(state));
    }

    [Fact]
    public async Task ToggleUnderlineCommand_AppliesUnderlineToSelectedText()
    {
        await _app.InvokeAsync("richtextbox.probe.create-plain", "underline me");

        var state = await _app.InvokeAsync("richtextbox.probe.toggle-underline-selection-command");
        var raw = state.ToString();

        Assert.True(HasRichTextBox(state), raw);
        Assert.True(HasDocument(state), raw);
        Assert.Contains("underline me", SelectionText(state));
        Assert.True(FirstInlineHasUnderline(state), raw);
    }

    [Fact]
    public async Task KeyDown_Backspace_RemovesPreviousCharacter()
    {
        await _app.InvokeAsync("richtextbox.probe.create-plain", "");
        await _app.InvokeAsync("richtextbox.probe.text-input-event", "abc");

        var state = await _app.InvokeAsync("richtextbox.probe.key-down", "Back");
        var raw = state.ToString();
        var text = Text(state);

        Assert.True(HasRichTextBox(state), raw);
        Assert.True(HasDocument(state), raw);
        Assert.Contains("ab", text);
        Assert.DoesNotContain("abc", text);
    }

    [Fact]
    public async Task KeyDown_Delete_RemovesSelectedText()
    {
        await _app.InvokeAsync("richtextbox.probe.create-plain", "");
        await _app.InvokeAsync("richtextbox.probe.text-input-event", "abc");

        var state = await _app.InvokeAsync("richtextbox.probe.key-down-select-all", "Delete");
        var raw = state.ToString();
        var text = Text(state);

        Assert.True(HasRichTextBox(state), raw);
        Assert.True(HasDocument(state), raw);
        Assert.DoesNotContain("abc", text);
    }

    [Fact]
    public async Task KeyDown_Enter_InsertsParagraphBreak()
    {
        await _app.InvokeAsync("richtextbox.probe.create-plain", "");
        await _app.InvokeAsync("richtextbox.probe.text-input-event", "abc");
        await _app.InvokeAsync("richtextbox.probe.key-down", "Enter");

        var state = await _app.InvokeAsync("richtextbox.probe.text-input-event", "def");
        var raw = state.ToString();
        var text = Text(state);

        Assert.True(HasRichTextBox(state), raw);
        Assert.True(HasDocument(state), raw);
        Assert.True(BlockCount(state) >= 2, raw);
        Assert.Contains("abc", text);
        Assert.Contains("def", text);
    }

    [Fact]
    public async Task SetDocument_ReadsParagraphRunText()
    {
        var state = await _app.InvokeAsync("richtextbox.probe.set-document", "document text");
        var raw = state.ToString();

        Assert.True(HasRichTextBox(state), raw);
        Assert.True(HasDocument(state), raw);
        Assert.True(state.GetProperty("blockCount").GetInt32() >= 1, raw);
        Assert.Contains("document text", Text(state));
    }
}
