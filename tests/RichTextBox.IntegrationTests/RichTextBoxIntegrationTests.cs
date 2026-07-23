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
    static int? SelectionStartRunOffset(JsonElement state) =>
        state.GetProperty("selectionStartRunOffset").ValueKind == JsonValueKind.Null
            ? null
            : state.GetProperty("selectionStartRunOffset").GetInt32();
    static int? SelectionEndRunOffset(JsonElement state) =>
        state.GetProperty("selectionEndRunOffset").ValueKind == JsonValueKind.Null
            ? null
            : state.GetProperty("selectionEndRunOffset").GetInt32();
    static string ClipboardText(JsonElement state) => state.GetProperty("clipboardText").GetString() ?? "";
    static string? FirstParagraphTextAlignment(JsonElement state) => state.GetProperty("firstParagraphTextAlignment").GetString();
    static string? FirstParagraphLineHeight(JsonElement state) => state.GetProperty("firstParagraphLineHeight").GetString();
    static string? FirstParagraphLineStackingStrategy(JsonElement state) => state.GetProperty("firstParagraphLineStackingStrategy").GetString();
    static string? FirstParagraphFlowDirection(JsonElement state) => state.GetProperty("firstParagraphFlowDirection").GetString();
    static string? FirstInlineFontWeight(JsonElement state) => state.GetProperty("firstInlineFontWeight").GetString();
    static string? FirstInlineFontStyle(JsonElement state) => state.GetProperty("firstInlineFontStyle").GetString();
    static string? FirstInlineFontSize(JsonElement state) => state.GetProperty("firstInlineFontSize").GetString();
    static string? FirstInlineFontFamily(JsonElement state) => state.GetProperty("firstInlineFontFamily").GetString();
    static string? FirstInlineForeground(JsonElement state) => state.GetProperty("firstInlineForeground").GetString();
    static string? FirstInlineBackground(JsonElement state) => state.GetProperty("firstInlineBackground").GetString();
    static string? FirstInlineFlowDirection(JsonElement state) => state.GetProperty("firstInlineFlowDirection").GetString();
    static bool FirstInlineHasUnderline(JsonElement state) => state.GetProperty("firstInlineHasUnderline").GetBoolean();
    static string? FirstRunFontWeight(JsonElement state) => state.GetProperty("firstRunFontWeight").GetString();
    static string? FirstRunFontStyle(JsonElement state) => state.GetProperty("firstRunFontStyle").GetString();
    static string? FirstRunFontSize(JsonElement state) => state.GetProperty("firstRunFontSize").GetString();
    static string? FirstRunFontFamily(JsonElement state) => state.GetProperty("firstRunFontFamily").GetString();
    static string? FirstRunForeground(JsonElement state) => state.GetProperty("firstRunForeground").GetString();
    static string? FirstRunBackground(JsonElement state) => state.GetProperty("firstRunBackground").GetString();
    static string? FirstRunFlowDirection(JsonElement state) => state.GetProperty("firstRunFlowDirection").GetString();
    static bool FirstRunHasUnderline(JsonElement state) => state.GetProperty("firstRunHasUnderline").GetBoolean();
    static string InlineTree(JsonElement state) => state.GetProperty("inlineTree").GetString() ?? "";
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
        Assert.True(state.TryGetProperty("canUndo", out _), state.ToString());
        Assert.True(state.TryGetProperty("canRedo", out _), state.ToString());
        Assert.True(state.TryGetProperty("selectionText", out _), state.ToString());
        Assert.True(state.TryGetProperty("selectionFontWeight", out _), state.ToString());
        Assert.True(state.TryGetProperty("selectionStartRunOffset", out _), state.ToString());
        Assert.True(state.TryGetProperty("selectionEndRunOffset", out _), state.ToString());
        Assert.True(state.TryGetProperty("clipboardText", out _), state.ToString());
        Assert.True(state.TryGetProperty("firstParagraphTextAlignment", out _), state.ToString());
        Assert.True(state.TryGetProperty("firstParagraphLineHeight", out _), state.ToString());
        Assert.True(state.TryGetProperty("firstParagraphLineStackingStrategy", out _), state.ToString());
        Assert.True(state.TryGetProperty("firstParagraphFlowDirection", out _), state.ToString());
        Assert.True(state.TryGetProperty("firstInlineType", out _), state.ToString());
        Assert.True(state.TryGetProperty("firstInlineFontWeight", out _), state.ToString());
        Assert.True(state.TryGetProperty("firstInlineFontStyle", out _), state.ToString());
        Assert.True(state.TryGetProperty("firstInlineFontSize", out _), state.ToString());
        Assert.True(state.TryGetProperty("firstInlineFontFamily", out _), state.ToString());
        Assert.True(state.TryGetProperty("firstInlineForeground", out _), state.ToString());
        Assert.True(state.TryGetProperty("firstInlineBackground", out _), state.ToString());
        Assert.True(state.TryGetProperty("firstInlineFlowDirection", out _), state.ToString());
        Assert.True(state.TryGetProperty("firstInlineHasUnderline", out _), state.ToString());
        Assert.True(state.TryGetProperty("inlineTree", out _), state.ToString());
        Assert.True(state.TryGetProperty("firstRunFontWeight", out _), state.ToString());
        Assert.True(state.TryGetProperty("firstRunFontStyle", out _), state.ToString());
        Assert.True(state.TryGetProperty("firstRunFontSize", out _), state.ToString());
        Assert.True(state.TryGetProperty("firstRunFontFamily", out _), state.ToString());
        Assert.True(state.TryGetProperty("firstRunForeground", out _), state.ToString());
        Assert.True(state.TryGetProperty("firstRunBackground", out _), state.ToString());
        Assert.True(state.TryGetProperty("firstRunFlowDirection", out _), state.ToString());
        Assert.True(state.TryGetProperty("firstRunHasUnderline", out _), state.ToString());
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
        Assert.True(FirstRunFontWeight(state) == "700", raw);
    }

    [Fact]
    public async Task ToggleBoldCommand_WhenInvokedTwice_RestoresNormalWeight()
    {
        await _app.InvokeAsync("richtextbox.probe.create-plain", "bold twice");
        await _app.InvokeAsync("richtextbox.probe.toggle-bold-selection-command");

        var state = await _app.InvokeAsync("richtextbox.probe.toggle-bold-selection-command");
        var raw = state.ToString();

        Assert.True(HasRichTextBox(state), raw);
        Assert.True(HasDocument(state), raw);
        Assert.Contains("bold twice", SelectionText(state));
        Assert.NotEqual("700", FirstRunFontWeight(state));
    }

    [Fact]
    public async Task ToggleBoldCommand_WithPartialRunSelection_SplitsOnlySelectedText()
    {
        await _app.InvokeAsync("richtextbox.probe.create-plain", "abcdef");

        var state = await _app.InvokeAsync("richtextbox.probe.toggle-bold-run-range-command", 2, 2);
        var raw = state.ToString();
        var inlineTree = InlineTree(state);

        Assert.True(HasRichTextBox(state), raw);
        Assert.True(HasDocument(state), raw);
        Assert.Equal("cd", SelectionText(state));
        Assert.Contains("Run:ab:", inlineTree);
        Assert.Contains("Run:cd:w=700", inlineTree);
        Assert.Contains("Run:ef:", inlineTree);
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
        Assert.Equal("Italic", FirstRunFontStyle(state));
    }

    [Fact]
    public async Task ToggleItalicCommand_WhenInvokedTwice_RestoresNormalStyle()
    {
        await _app.InvokeAsync("richtextbox.probe.create-plain", "italic twice");
        await _app.InvokeAsync("richtextbox.probe.toggle-italic-selection-command");

        var state = await _app.InvokeAsync("richtextbox.probe.toggle-italic-selection-command");
        var raw = state.ToString();

        Assert.True(HasRichTextBox(state), raw);
        Assert.True(HasDocument(state), raw);
        Assert.Contains("italic twice", SelectionText(state));
        Assert.NotEqual("Italic", FirstRunFontStyle(state));
    }

    [Fact]
    public async Task ToggleItalicCommand_WithPartialRunSelection_SplitsOnlySelectedText()
    {
        await _app.InvokeAsync("richtextbox.probe.create-plain", "abcdef");

        var state = await _app.InvokeAsync("richtextbox.probe.toggle-italic-run-range-command", 2, 2);
        var raw = state.ToString();
        var inlineTree = InlineTree(state);

        Assert.True(HasRichTextBox(state), raw);
        Assert.True(HasDocument(state), raw);
        Assert.Equal("cd", SelectionText(state));
        Assert.Contains("Run:ab:", inlineTree);
        Assert.Contains("Run:cd:w=400:s=Italic", inlineTree);
        Assert.Contains("Run:ef:", inlineTree);
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
        Assert.True(FirstRunHasUnderline(state), raw);
    }

    [Fact]
    public async Task ToggleUnderlineCommand_WhenInvokedTwice_RemovesUnderline()
    {
        await _app.InvokeAsync("richtextbox.probe.create-plain", "underline twice");
        await _app.InvokeAsync("richtextbox.probe.toggle-underline-selection-command");

        var state = await _app.InvokeAsync("richtextbox.probe.toggle-underline-selection-command");
        var raw = state.ToString();

        Assert.True(HasRichTextBox(state), raw);
        Assert.True(HasDocument(state), raw);
        Assert.Contains("underline twice", SelectionText(state));
        Assert.False(FirstRunHasUnderline(state), raw);
    }

    [Fact]
    public async Task ToggleUnderlineCommand_WithPartialRunSelection_SplitsOnlySelectedText()
    {
        await _app.InvokeAsync("richtextbox.probe.create-plain", "abcdef");

        var state = await _app.InvokeAsync("richtextbox.probe.toggle-underline-run-range-command", 2, 2);
        var raw = state.ToString();
        var inlineTree = InlineTree(state);

        Assert.True(HasRichTextBox(state), raw);
        Assert.True(HasDocument(state), raw);
        Assert.Equal("cd", SelectionText(state));
        Assert.Contains("Run:ab:", inlineTree);
        Assert.Contains("Run:cd:w=400:s=Normal:z=14:d=U", inlineTree);
        Assert.Contains("Run:ef:", inlineTree);
    }

    [Fact]
    public async Task KeyDown_ControlB_AppliesBoldToSelectedText()
    {
        await _app.InvokeAsync("richtextbox.probe.create-plain", "ctrl bold");

        var state = await _app.InvokeAsync("richtextbox.probe.key-down-select-all-modifiers", "B", "Control");
        var raw = state.ToString();

        Assert.True(HasRichTextBox(state), raw);
        Assert.True(HasDocument(state), raw);
        Assert.Contains("ctrl bold", SelectionText(state));
        Assert.Equal("700", FirstRunFontWeight(state));
    }

    [Fact]
    public async Task KeyDown_ControlI_AppliesItalicToSelectedText()
    {
        await _app.InvokeAsync("richtextbox.probe.create-plain", "ctrl italic");

        var state = await _app.InvokeAsync("richtextbox.probe.key-down-select-all-modifiers", "I", "Control");
        var raw = state.ToString();

        Assert.True(HasRichTextBox(state), raw);
        Assert.True(HasDocument(state), raw);
        Assert.Contains("ctrl italic", SelectionText(state));
        Assert.Equal("Italic", FirstRunFontStyle(state));
    }

    [Fact]
    public async Task KeyDown_ControlU_AppliesUnderlineToSelectedText()
    {
        await _app.InvokeAsync("richtextbox.probe.create-plain", "ctrl underline");

        var state = await _app.InvokeAsync("richtextbox.probe.key-down-select-all-modifiers", "U", "Control");
        var raw = state.ToString();

        Assert.True(HasRichTextBox(state), raw);
        Assert.True(HasDocument(state), raw);
        Assert.Contains("ctrl underline", SelectionText(state));
        Assert.True(FirstRunHasUnderline(state), raw);
    }

    [Fact]
    public async Task ApplyFontSizeCommand_AppliesFontSizeToSelectedText()
    {
        await _app.InvokeAsync("richtextbox.probe.create-plain", "size me");

        var state = await _app.InvokeAsync("richtextbox.probe.apply-font-size-selection-command", 24);
        var raw = state.ToString();

        Assert.True(HasRichTextBox(state), raw);
        Assert.True(HasDocument(state), raw);
        Assert.Contains("size me", SelectionText(state));
        Assert.Equal("24", FirstRunFontSize(state));
    }

    [Fact]
    public async Task IncreaseFontSizeCommand_IncreasesSelectedTextFontSize()
    {
        await _app.InvokeAsync("richtextbox.probe.create-plain", "bigger");
        await _app.InvokeAsync("richtextbox.probe.apply-font-size-selection-command", 24);

        var state = await _app.InvokeAsync("richtextbox.probe.increase-font-size-selection-command");
        var raw = state.ToString();

        Assert.True(HasRichTextBox(state), raw);
        Assert.True(HasDocument(state), raw);
        Assert.Contains("bigger", SelectionText(state));
        Assert.Equal("24.75", FirstRunFontSize(state));
    }

    [Fact]
    public async Task DecreaseFontSizeCommand_DecreasesSelectedTextFontSize()
    {
        await _app.InvokeAsync("richtextbox.probe.create-plain", "smaller");
        await _app.InvokeAsync("richtextbox.probe.apply-font-size-selection-command", 24);

        var state = await _app.InvokeAsync("richtextbox.probe.decrease-font-size-selection-command");
        var raw = state.ToString();

        Assert.True(HasRichTextBox(state), raw);
        Assert.True(HasDocument(state), raw);
        Assert.Contains("smaller", SelectionText(state));
        Assert.Equal("23.25", FirstRunFontSize(state));
    }

    [Fact]
    public async Task ApplyFontFamilyCommand_AppliesFontFamilyToSelectedText()
    {
        await _app.InvokeAsync("richtextbox.probe.create-plain", "family");

        var state = await _app.InvokeAsync("richtextbox.probe.apply-font-family-selection-command", "Courier New");
        var raw = state.ToString();

        Assert.True(HasRichTextBox(state), raw);
        Assert.True(HasDocument(state), raw);
        Assert.Contains("family", SelectionText(state));
        Assert.Equal("Courier New", FirstRunFontFamily(state));
    }

    [Fact]
    public async Task ApplyForegroundCommand_AppliesForegroundToSelectedText()
    {
        await _app.InvokeAsync("richtextbox.probe.create-plain", "foreground");

        var state = await _app.InvokeAsync("richtextbox.probe.apply-foreground-selection-command");
        var raw = state.ToString();

        Assert.True(HasRichTextBox(state), raw);
        Assert.True(HasDocument(state), raw);
        Assert.Contains("foreground", SelectionText(state));
        Assert.Equal("#FF90EE90", FirstRunForeground(state));
    }

    [Fact]
    public async Task ApplyBackgroundCommand_AppliesBackgroundToSelectedText()
    {
        await _app.InvokeAsync("richtextbox.probe.create-plain", "background");

        var state = await _app.InvokeAsync("richtextbox.probe.apply-background-selection-command");
        var raw = state.ToString();

        Assert.True(HasRichTextBox(state), raw);
        Assert.True(HasDocument(state), raw);
        Assert.Contains("background", SelectionText(state));
        Assert.Equal("#FFFFB6C1", FirstRunBackground(state));
    }

    [Theory]
    [InlineData("richtextbox.probe.align-left-selection-command", "Left")]
    [InlineData("richtextbox.probe.align-center-selection-command", "Center")]
    [InlineData("richtextbox.probe.align-right-selection-command", "Right")]
    [InlineData("richtextbox.probe.align-justify-selection-command", "Justify")]
    public async Task AlignCommand_AppliesTextAlignmentToSelectedParagraph(string action, string expectedAlignment)
    {
        await _app.InvokeAsync("richtextbox.probe.create-plain", "align me");

        var state = await _app.InvokeAsync(action);
        var raw = state.ToString();

        Assert.True(HasRichTextBox(state), raw);
        Assert.True(HasDocument(state), raw);
        Assert.Contains("align me", SelectionText(state));
        Assert.Equal(expectedAlignment, FirstParagraphTextAlignment(state));
    }

    [Theory]
    [InlineData("richtextbox.probe.apply-single-space-selection-command")]
    [InlineData("richtextbox.probe.apply-one-and-a-half-space-selection-command")]
    [InlineData("richtextbox.probe.apply-double-space-selection-command")]
    public async Task LineSpacingCommand_MatchesWpfNoOpBehavior(string action)
    {
        await _app.InvokeAsync("richtextbox.probe.create-plain", "spacing");

        var state = await _app.InvokeAsync(action);
        var raw = state.ToString();

        Assert.True(HasRichTextBox(state), raw);
        Assert.True(HasDocument(state), raw);
        Assert.Contains("spacing", SelectionText(state));
        Assert.Equal("NaN", FirstParagraphLineHeight(state));
        Assert.Equal("MaxHeight", FirstParagraphLineStackingStrategy(state));
    }

    [Theory]
    [InlineData("richtextbox.probe.apply-paragraph-flow-direction-ltr-selection-command", "LeftToRight")]
    [InlineData("richtextbox.probe.apply-paragraph-flow-direction-rtl-selection-command", "RightToLeft")]
    public async Task ParagraphFlowDirectionCommand_AppliesFlowDirectionToSelectedParagraph(string action, string expectedDirection)
    {
        await _app.InvokeAsync("richtextbox.probe.create-plain", "direction");

        var state = await _app.InvokeAsync(action);
        var raw = state.ToString();

        Assert.True(HasRichTextBox(state), raw);
        Assert.True(HasDocument(state), raw);
        Assert.Contains("direction", SelectionText(state));
        Assert.Equal(expectedDirection, FirstParagraphFlowDirection(state));
    }

    [Theory]
    [InlineData("richtextbox.probe.apply-inline-flow-direction-ltr-selection-command", "LeftToRight")]
    [InlineData("richtextbox.probe.apply-inline-flow-direction-rtl-selection-command", "RightToLeft")]
    public async Task InlineFlowDirectionCommand_AppliesFlowDirectionToSelectedText(string action, string expectedDirection)
    {
        await _app.InvokeAsync("richtextbox.probe.create-plain", "inline direction");

        var state = await _app.InvokeAsync(action);
        var raw = state.ToString();

        Assert.True(HasRichTextBox(state), raw);
        Assert.True(HasDocument(state), raw);
        Assert.Contains("inline direction", SelectionText(state));
        Assert.Equal(expectedDirection, FirstRunFlowDirection(state));
    }

    [Fact]
    public async Task KeyDownUp_ControlLeftShift_AppliesParagraphFlowDirectionLeftToRight()
    {
        await _app.InvokeAsync("richtextbox.probe.create-plain", "keyboard direction");
        await _app.InvokeAsync("richtextbox.probe.apply-paragraph-flow-direction-rtl-selection-command");

        var state = await _app.InvokeAsync("richtextbox.probe.key-down-up-select-all-modifiers", "LeftShift", "Control");
        var raw = state.ToString();

        Assert.True(HasRichTextBox(state), raw);
        Assert.True(HasDocument(state), raw);
        Assert.Contains("keyboard direction", SelectionText(state));
        Assert.Equal("LeftToRight", FirstParagraphFlowDirection(state));
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
    public async Task KeyDown_DeleteAtParagraphEnd_MergesNextParagraph()
    {
        await _app.InvokeAsync("richtextbox.probe.create-plain", "");
        await _app.InvokeAsync("richtextbox.probe.text-input-event", "abc");
        await _app.InvokeAsync("richtextbox.probe.key-down", "Enter");
        await _app.InvokeAsync("richtextbox.probe.text-input-event", "def");
        var beforeState = await _app.InvokeAsync("richtextbox.probe.state");
        Assert.True(BlockCount(beforeState) >= 2, beforeState.ToString());

        // Caret at offset 3 inside the first Run ("abc") sits at the end of the
        // first paragraph, right before the paragraph break.
        await _app.InvokeAsync("richtextbox.probe.set-caret-run-offset", 3);

        var state = await _app.InvokeAsync("richtextbox.probe.key-down", "Delete");
        var raw = state.ToString();
        var text = Text(state);

        Assert.True(HasRichTextBox(state), raw);
        Assert.True(HasDocument(state), raw);
        Assert.Equal(1, BlockCount(state));
        Assert.Contains("abcdef", text);
    }

    [Fact]
    public async Task KeyDown_BackspaceAtParagraphStart_MergesPreviousParagraph()
    {
        await _app.InvokeAsync("richtextbox.probe.create-plain", "");
        await _app.InvokeAsync("richtextbox.probe.text-input-event", "abc");
        await _app.InvokeAsync("richtextbox.probe.key-down", "Enter");
        await _app.InvokeAsync("richtextbox.probe.text-input-event", "def");
        var beforeState = await _app.InvokeAsync("richtextbox.probe.state");
        Assert.True(BlockCount(beforeState) >= 2, beforeState.ToString());

        // Move to document end, then Home to reach the start of the second
        // paragraph's line, so Backspace merges across the paragraph break.
        await _app.InvokeAsync("richtextbox.probe.key-down-modifiers", "End", "Control");
        await _app.InvokeAsync("richtextbox.probe.key-down", "Home");
        var state = await _app.InvokeAsync("richtextbox.probe.key-down", "Back");
        var raw = state.ToString();
        var text = Text(state);

        Assert.True(HasRichTextBox(state), raw);
        Assert.True(HasDocument(state), raw);
        Assert.Equal(1, BlockCount(state));
        Assert.Contains("abcdef", text);
    }

    [Fact]
    public async Task CopyRunRange_WritesSelectionToClipboardWithoutChangingDocument()
    {
        await _app.InvokeAsync("richtextbox.probe.create-plain", "copy text");

        var state = await _app.InvokeAsync("richtextbox.probe.copy-run-range", 0, 4);
        var raw = state.ToString();

        Assert.True(HasRichTextBox(state), raw);
        Assert.True(HasDocument(state), raw);
        Assert.Contains("copy text", Text(state));
        Assert.Equal("copy", SelectionText(state));
        Assert.Equal("copy", ClipboardText(state));
    }

    [Fact]
    public async Task CutRunRange_WritesSelectionToClipboardAndDeletesSelection()
    {
        await _app.InvokeAsync("richtextbox.probe.create-plain", "cut text");

        var state = await _app.InvokeAsync("richtextbox.probe.cut-run-range", 0, 3);
        var raw = state.ToString();

        Assert.True(HasRichTextBox(state), raw);
        Assert.True(HasDocument(state), raw);
        Assert.DoesNotContain("cut text", Text(state));
        Assert.Contains(" text", Text(state));
        Assert.Equal("cut", ClipboardText(state));
    }

    [Fact]
    public async Task PasteText_InsertsClipboardTextAtCurrentSelection()
    {
        await _app.InvokeAsync("richtextbox.probe.create-plain", "prefix ");

        var state = await _app.InvokeAsync("richtextbox.probe.paste-text-at-run-offset", "pasted", 7);
        var raw = state.ToString();

        Assert.True(HasRichTextBox(state), raw);
        Assert.True(HasDocument(state), raw);
        Assert.Contains("prefix pasted", Text(state));
        Assert.Equal("pasted", ClipboardText(state));
    }

    [Fact]
    public async Task KeyDown_LeftRight_MovesCaretByCharacter()
    {
        await _app.InvokeAsync("richtextbox.probe.create-plain", "abc");
        await _app.InvokeAsync("richtextbox.probe.set-caret-run-offset", 1);

        var right = await _app.InvokeAsync("richtextbox.probe.key-down", "Right");
        var rightRaw = right.ToString();
        Assert.True(HasRichTextBox(right), rightRaw);
        Assert.True(HasDocument(right), rightRaw);
        Assert.Equal("", SelectionText(right));
        Assert.Equal(2, SelectionStartRunOffset(right));
        Assert.Equal(2, SelectionEndRunOffset(right));

        var left = await _app.InvokeAsync("richtextbox.probe.key-down", "Left");
        var leftRaw = left.ToString();
        Assert.True(HasRichTextBox(left), leftRaw);
        Assert.True(HasDocument(left), leftRaw);
        Assert.Equal("", SelectionText(left));
        Assert.Equal(1, SelectionStartRunOffset(left));
        Assert.Equal(1, SelectionEndRunOffset(left));
    }

    [Fact]
    public async Task KeyDown_ShiftRight_ExtendsSelectionByCharacter()
    {
        await _app.InvokeAsync("richtextbox.probe.create-plain", "abc");
        await _app.InvokeAsync("richtextbox.probe.set-caret-run-offset", 1);

        var state = await _app.InvokeAsync("richtextbox.probe.key-down-modifiers", "Right", "Shift");
        var raw = state.ToString();

        Assert.True(HasRichTextBox(state), raw);
        Assert.True(HasDocument(state), raw);
        Assert.Equal("b", SelectionText(state));
        Assert.Equal(1, SelectionStartRunOffset(state));
        Assert.Equal(2, SelectionEndRunOffset(state));
    }

    [Theory]
    [InlineData("Home", "", 0)]
    [InlineData("End", "", 3)]
    [InlineData("Home", "Control", 0)]
    [InlineData("End", "Control", 3)]
    public async Task KeyDown_BoundaryKeys_MoveCaretToExpectedBoundary(string key, string modifiers, int expectedOffset)
    {
        await _app.InvokeAsync("richtextbox.probe.create-plain", "abc");
        await _app.InvokeAsync("richtextbox.probe.set-caret-run-offset", 1);

        var state = string.IsNullOrEmpty(modifiers)
            ? await _app.InvokeAsync("richtextbox.probe.key-down", key)
            : await _app.InvokeAsync("richtextbox.probe.key-down-modifiers", key, modifiers);
        var raw = state.ToString();

        Assert.True(HasRichTextBox(state), raw);
        Assert.True(HasDocument(state), raw);
        Assert.Equal("", SelectionText(state));
        Assert.Equal(expectedOffset, SelectionStartRunOffset(state));
        Assert.Equal(expectedOffset, SelectionEndRunOffset(state));
    }

    [Theory]
    [InlineData("Home", "Shift", "a", 0, 1)]
    [InlineData("End", "Shift", "bc", 1, 3)]
    [InlineData("Home", "Control,Shift", "a", 0, 1)]
    [InlineData("End", "Control,Shift", "bc\n", 1, 5)]
    public async Task KeyDown_ShiftBoundaryKeys_ExtendSelectionToExpectedBoundary(
        string key,
        string modifiers,
        string expectedSelection,
        int expectedStart,
        int expectedEnd)
    {
        await _app.InvokeAsync("richtextbox.probe.create-plain", "abc");
        await _app.InvokeAsync("richtextbox.probe.set-caret-run-offset", 1);

        var state = await _app.InvokeAsync("richtextbox.probe.key-down-modifiers", key, modifiers);
        var raw = state.ToString();

        Assert.True(HasRichTextBox(state), raw);
        Assert.True(HasDocument(state), raw);
        Assert.Equal(expectedSelection, SelectionText(state));
        Assert.Equal(expectedStart, SelectionStartRunOffset(state));
        Assert.Equal(expectedEnd, SelectionEndRunOffset(state));
    }

    [Theory]
    [InlineData("Right", 5, 8)]
    [InlineData("Left", 5, 4)]
    public async Task KeyDown_ControlLeftRight_MovesCaretByWord(string key, int initialOffset, int expectedOffset)
    {
        await _app.InvokeAsync("richtextbox.probe.create-plain", "one two three");
        await _app.InvokeAsync("richtextbox.probe.set-caret-run-offset", initialOffset);

        var state = await _app.InvokeAsync("richtextbox.probe.key-down-modifiers", key, "Control");
        var raw = state.ToString();

        Assert.True(HasRichTextBox(state), raw);
        Assert.True(HasDocument(state), raw);
        Assert.Equal("", SelectionText(state));
        Assert.Equal(expectedOffset, SelectionStartRunOffset(state));
        Assert.Equal(expectedOffset, SelectionEndRunOffset(state));
    }

    [Theory]
    [InlineData("Right", "w", 5, 6)]
    [InlineData("Left", "t", 4, 5)]
    public async Task KeyDown_ControlShiftLeftRight_ExtendsSelectionByWord(
        string key,
        string expectedSelection,
        int expectedStart,
        int expectedEnd)
    {
        await _app.InvokeAsync("richtextbox.probe.create-plain", "one two three");
        await _app.InvokeAsync("richtextbox.probe.set-caret-run-offset", 5);

        var state = await _app.InvokeAsync("richtextbox.probe.key-down-modifiers", key, "Control,Shift");
        var raw = state.ToString();

        Assert.True(HasRichTextBox(state), raw);
        Assert.True(HasDocument(state), raw);
        Assert.Equal(expectedSelection, SelectionText(state));
        Assert.Equal(expectedStart, SelectionStartRunOffset(state));
        Assert.Equal(expectedEnd, SelectionEndRunOffset(state));
    }

    [Theory]
    [InlineData("Delete", "one tthree", 5)]
    [InlineData("Back", "one wo three", 4)]
    public async Task KeyDown_ControlBackspaceDelete_DeletesWordBoundaryRange(
        string key,
        string expectedText,
        int expectedCaretOffset)
    {
        await _app.InvokeAsync("richtextbox.probe.create-plain", "one two three");
        await _app.InvokeAsync("richtextbox.probe.set-caret-run-offset", 5);

        var state = await _app.InvokeAsync("richtextbox.probe.key-down-modifiers", key, "Control");
        var raw = state.ToString();

        Assert.True(HasRichTextBox(state), raw);
        Assert.True(HasDocument(state), raw);
        Assert.Contains(expectedText, Text(state));
        Assert.Equal("", SelectionText(state));
        Assert.Equal(expectedCaretOffset, SelectionStartRunOffset(state));
        Assert.Equal(expectedCaretOffset, SelectionEndRunOffset(state));
    }

    [Theory]
    [InlineData("Delete", "one three", 4)]
    [InlineData("Back", "one three", 4)]
    public async Task KeyDown_ControlBackspaceDelete_WithNonEmptySelection_DeletesSelectionWithoutExpandingToWordBoundary(
        string key,
        string expectedText,
        int expectedCaretOffset)
    {
        await _app.InvokeAsync("richtextbox.probe.create-plain", "one two three");
        // Select "two " (offsets 4-8) inside the first Run so the selection sits
        // strictly inside word boundaries on both sides.
        await _app.InvokeAsync("richtextbox.probe.select-run-range", 4, 4);

        var state = await _app.InvokeAsync("richtextbox.probe.key-down-modifiers", key, "Control");
        var raw = state.ToString();

        Assert.True(HasRichTextBox(state), raw);
        Assert.True(HasDocument(state), raw);
        Assert.Contains(expectedText, Text(state));
        Assert.Equal("", SelectionText(state));
        Assert.Equal(expectedCaretOffset, SelectionStartRunOffset(state));
        Assert.Equal(expectedCaretOffset, SelectionEndRunOffset(state));
    }

    [Fact]
    public async Task UndoRedo_RestoresTextInputMutation()
    {
        await _app.InvokeAsync("richtextbox.probe.create-plain", "");
        await _app.InvokeAsync("richtextbox.probe.text-input-event", "abc");

        var undo = await _app.InvokeAsync("richtextbox.probe.undo");
        var undoRaw = undo.ToString();
        Assert.True(HasRichTextBox(undo), undoRaw);
        Assert.True(HasDocument(undo), undoRaw);
        Assert.DoesNotContain("abc", Text(undo));
        Assert.True(undo.GetProperty("canRedo").GetBoolean(), undoRaw);

        var redo = await _app.InvokeAsync("richtextbox.probe.redo");
        var redoRaw = redo.ToString();
        Assert.True(HasRichTextBox(redo), redoRaw);
        Assert.True(HasDocument(redo), redoRaw);
        Assert.Contains("abc", Text(redo));
    }

    [Fact]
    public async Task KeyDown_ControlZAndControlY_InvokeUndoRedo()
    {
        await _app.InvokeAsync("richtextbox.probe.create-plain", "");
        await _app.InvokeAsync("richtextbox.probe.text-input-event", "abc");

        var undo = await _app.InvokeAsync("richtextbox.probe.key-down-select-all-modifiers", "Z", "Control");
        var undoRaw = undo.ToString();
        Assert.True(HasRichTextBox(undo), undoRaw);
        Assert.True(HasDocument(undo), undoRaw);
        Assert.DoesNotContain("abc", Text(undo));

        var redo = await _app.InvokeAsync("richtextbox.probe.key-down-select-all-modifiers", "Y", "Control");
        var redoRaw = redo.ToString();
        Assert.True(HasRichTextBox(redo), redoRaw);
        Assert.True(HasDocument(redo), redoRaw);
        Assert.Contains("abc", Text(redo));
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
