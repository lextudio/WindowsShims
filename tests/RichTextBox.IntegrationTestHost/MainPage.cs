#if DEBUG
using System.Threading;
using LeXtudio.DevFlow.Agent.Core;
using Microsoft.UI.Xaml.Controls;
using RichTextBox.TestScenarios;
using WpfDocumentEditingCommands = System.Windows.Documents.EditingCommands;
using WpfExecutedRoutedEventArgs = System.Windows.Input.ExecutedRoutedEventArgs;
using WpfEditingCommands = System.Windows.Input.EditingCommands;
using WpfInline = System.Windows.Documents.Inline;
using WpfParagraph = System.Windows.Documents.Paragraph;
using WpfRun = System.Windows.Documents.Run;
using WpfSpan = System.Windows.Documents.Span;
using WpfTextDecorationCollection = System.Windows.Media.TextDecorationCollection;
using WpfTextDecorations = System.Windows.Media.TextDecorations;
using WpfTextCompositionEventArgs = System.Windows.Input.TextCompositionEventArgs;
using WpfTextElement = System.Windows.Documents.TextElement;
using WpfRichTextBox = System.Windows.Controls.RichTextBox;
using WpfTextRange = System.Windows.Documents.TextRange;
#endif

namespace RichTextBox.IntegrationTestHost;

#if DEBUG
public sealed partial class MainPage : Page
{
    private static MainPage? _current;
    private readonly Grid _root;
    private WpfRichTextBox? _box;

    public MainPage()
    {
        _current = this;
        _root = new Grid();
        Content = _root;
    }

    static string Js(string? s) =>
        s is null ? "null" : $"\"{s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t")}\"";

    static string Jb(bool b) => b ? "true" : "false";

    static string? FormatFontWeight(object? value)
    {
        if (value is null)
            return null;

        var weight = value.GetType()
            .GetProperty("Weight", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
            ?.GetValue(value)
            ?? value.GetType()
            .GetField("Weight", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
            ?.GetValue(value);
        return weight?.ToString() ?? value.ToString();
    }

    static bool HasUnderline(object? value)
    {
        if (value is not WpfTextDecorationCollection decorations)
            return false;

        foreach (var decoration in decorations)
        {
            foreach (var underline in WpfTextDecorations.Underline)
            {
                if (Equals(decoration, underline))
                    return true;
            }
        }

        return false;
    }

    static string? FormatBrush(object? value)
    {
        if (value is Microsoft.UI.Xaml.Media.SolidColorBrush brush)
        {
            var color = brush.Color;
            return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        return value?.ToString();
    }

    static string? FormatFontFamily(object? value)
    {
        if (value is Microsoft.UI.Xaml.Media.FontFamily family)
            return family.Source;

        return value?.ToString();
    }

    static WpfRun? FirstRun(WpfInline? inline)
    {
        while (inline is not null)
        {
            if (inline is WpfRun run)
                return run;

            inline = inline is WpfSpan span ? span.Inlines.FirstInline : null;
        }

        return null;
    }

    static string FormatInlineTree(WpfInline? inline)
    {
        var parts = new List<string>();
        AppendInlineTree(parts, inline);
        return string.Join("|", parts);
    }

    static string InlineText(string text) =>
        text.Replace("\\", "\\\\").Replace("|", "\\|").Replace(":", "\\:").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");

    static void AppendInlineTree(List<string> parts, WpfInline? inline)
    {
        while (inline is not null)
        {
            if (inline is WpfRun run)
            {
                var style = run.GetValue(WpfTextElement.FontStyleProperty)?.ToString();
                var size = run.GetValue(WpfTextElement.FontSizeProperty)?.ToString();
                var underline = HasUnderline(run.GetValue(WpfInline.TextDecorationsProperty)) ? "U" : "-";
                parts.Add($"Run:{InlineText(run.Text)}:w={FormatFontWeight(run.GetValue(WpfTextElement.FontWeightProperty))}:s={style}:z={size}:d={underline}");
            }
            else if (inline is WpfSpan span)
            {
                var style = span.GetValue(WpfTextElement.FontStyleProperty)?.ToString();
                var size = span.GetValue(WpfTextElement.FontSizeProperty)?.ToString();
                var underline = HasUnderline(span.GetValue(WpfInline.TextDecorationsProperty)) ? "U" : "-";
                parts.Add($"{span.GetType().Name}:w={FormatFontWeight(span.GetValue(WpfTextElement.FontWeightProperty))}:s={style}:z={size}:d={underline}");
                AppendInlineTree(parts, span.Inlines.FirstInline);
            }
            else
            {
                parts.Add(inline.GetType().Name);
            }

            inline = inline.NextInline;
        }
    }

    static string RunOnUi(Func<MainPage, string> body)
    {
        var page = _current;
        if (page is null) return "{\"error\":\"MainPage not available\"}";
        string result = "{\"error\":\"timeout\"}";
        using var done = new ManualResetEventSlim();
        page.DispatcherQueue.TryEnqueue(() =>
        {
            try { result = body(page); }
            catch (Exception ex)
            {
                var real = ex is System.Reflection.TargetInvocationException { InnerException: { } inner } ? inner : ex;
                result = $"{{\"error\":{Js(real.Message)},\"errorType\":{Js(real.GetType().FullName)},\"stack\":{Js(real.StackTrace)}}}";
            }
            finally { done.Set(); }
        });
        done.Wait(TimeSpan.FromSeconds(30));
        return result;
    }

    static string Snapshot(MainPage page)
    {
        var box = page._box;
        box?.UpdateLayout();
        var document = box?.Document;
        var text = document is null
            ? null
            : new WpfTextRange(document.ContentStart, document.ContentEnd).Text;
        var renderScope = box is null ? null : GetInternalProperty(box, "RenderScope");
        var textEditor = box is null ? null : GetInternalProperty(box, "TextEditor");
        var textView = textEditor is null ? null : GetInternalProperty(textEditor, "TextView");
        var contentHostAvailable = box is not null && (GetInternalProperty(box, "IsContentHostAvailable") as bool? ?? false);
        var selection = box?.Selection;
        var canUndo = box?.CanUndo ?? false;
        var canRedo = box?.CanRedo ?? false;
        var selectionText = selection?.Text;
        var selectionFontWeight = selection is null
            ? null
            : FormatFontWeight(selection.GetPropertyValue(WpfTextElement.FontWeightProperty));
        var firstParagraph = document?.Blocks.FirstBlock as WpfParagraph;
        var firstParagraphTextAlignment = firstParagraph?.TextAlignment.ToString();
        var firstParagraphLineHeight = firstParagraph?.LineHeight.ToString();
        var firstParagraphLineStackingStrategy = firstParagraph?.LineStackingStrategy.ToString();
        var firstParagraphFlowDirection = firstParagraph?.FlowDirection.ToString();
        var firstInline = firstParagraph is not null
            ? firstParagraph.Inlines.FirstInline
            : null;
        var firstRun = FirstRun(firstInline);
        var inlineTree = FormatInlineTree(firstInline);
        var firstInlineFontWeight = firstInline is null
            ? null
            : FormatFontWeight(firstInline.GetValue(WpfTextElement.FontWeightProperty));
        var firstInlineFontStyle = firstInline is null
            ? null
            : firstInline.GetValue(WpfTextElement.FontStyleProperty)?.ToString();
        var firstInlineFontSize = firstInline is null
            ? null
            : firstInline.GetValue(WpfTextElement.FontSizeProperty)?.ToString();
        var firstInlineFontFamily = firstInline is null
            ? null
            : FormatFontFamily(firstInline.GetValue(WpfTextElement.FontFamilyProperty));
        var firstInlineForeground = firstInline is null
            ? null
            : FormatBrush(firstInline.GetValue(WpfTextElement.ForegroundProperty));
        var firstInlineBackground = firstInline is null
            ? null
            : FormatBrush(firstInline.GetValue(WpfTextElement.BackgroundProperty));
        var firstInlineHasUnderline = firstInline is not null
            && HasUnderline(firstInline.GetValue(WpfInline.TextDecorationsProperty));
        var firstRunFontWeight = firstRun is null
            ? null
            : FormatFontWeight(firstRun.GetValue(WpfTextElement.FontWeightProperty));
        var firstRunFontStyle = firstRun is null
            ? null
            : firstRun.GetValue(WpfTextElement.FontStyleProperty)?.ToString();
        var firstRunFontSize = firstRun is null
            ? null
            : firstRun.GetValue(WpfTextElement.FontSizeProperty)?.ToString();
        var firstRunFontFamily = firstRun is null
            ? null
            : FormatFontFamily(firstRun.GetValue(WpfTextElement.FontFamilyProperty));
        var firstRunForeground = firstRun is null
            ? null
            : FormatBrush(firstRun.GetValue(WpfTextElement.ForegroundProperty));
        var firstRunBackground = firstRun is null
            ? null
            : FormatBrush(firstRun.GetValue(WpfTextElement.BackgroundProperty));
        var firstRunHasUnderline = firstRun is not null
            && HasUnderline(firstRun.GetValue(WpfInline.TextDecorationsProperty));

        return $"{{\"hasRichTextBox\":{Jb(box is not null)},\"hasDocument\":{Jb(document is not null)},\"blockCount\":{(document?.Blocks.Count ?? 0)},\"text\":{Js(text)},\"canUndo\":{Jb(canUndo)},\"canRedo\":{Jb(canRedo)},\"selectionText\":{Js(selectionText)},\"selectionFontWeight\":{Js(selectionFontWeight)},\"firstParagraphTextAlignment\":{Js(firstParagraphTextAlignment)},\"firstParagraphLineHeight\":{Js(firstParagraphLineHeight)},\"firstParagraphLineStackingStrategy\":{Js(firstParagraphLineStackingStrategy)},\"firstParagraphFlowDirection\":{Js(firstParagraphFlowDirection)},\"inlineTree\":{Js(inlineTree)},\"firstInlineType\":{Js(firstInline?.GetType().FullName)},\"firstInlineFontWeight\":{Js(firstInlineFontWeight)},\"firstInlineFontStyle\":{Js(firstInlineFontStyle)},\"firstInlineFontSize\":{Js(firstInlineFontSize)},\"firstInlineFontFamily\":{Js(firstInlineFontFamily)},\"firstInlineForeground\":{Js(firstInlineForeground)},\"firstInlineBackground\":{Js(firstInlineBackground)},\"firstInlineHasUnderline\":{Jb(firstInlineHasUnderline)},\"firstRunFontWeight\":{Js(firstRunFontWeight)},\"firstRunFontStyle\":{Js(firstRunFontStyle)},\"firstRunFontSize\":{Js(firstRunFontSize)},\"firstRunFontFamily\":{Js(firstRunFontFamily)},\"firstRunForeground\":{Js(firstRunForeground)},\"firstRunBackground\":{Js(firstRunBackground)},\"firstRunHasUnderline\":{Jb(firstRunHasUnderline)},\"contentHostAvailable\":{Jb(contentHostAvailable)},\"renderScopeType\":{Js(renderScope?.GetType().FullName)},\"textViewType\":{Js(textView?.GetType().FullName)}}}";
    }

    static object? GetInternalProperty(object instance, string name)
    {
        return instance.GetType()
            .GetProperty(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)
            ?.GetValue(instance);
    }

    static object RequireTextEditor(WpfRichTextBox box)
    {
        return GetInternalProperty(box, "TextEditor")
            ?? throw new InvalidOperationException("RichTextBox TextEditor is not available.");
    }

    static void SelectFirstRunTextRange(WpfRichTextBox box, int start, int length)
    {
        var document = box.Document ?? throw new InvalidOperationException("RichTextBox document is not available.");
        var paragraph = document.Blocks.FirstBlock as WpfParagraph
            ?? throw new InvalidOperationException("The first document block is not a Paragraph.");
        var run = FirstRun(paragraph.Inlines.FirstInline)
            ?? throw new InvalidOperationException("The first paragraph does not contain a Run.");
        if (start < 0 || length < 0 || start + length > run.Text.Length)
            throw new ArgumentOutOfRangeException(nameof(start), $"Range [{start}, {start + length}) is outside the first run length {run.Text.Length}.");

        var selectionStart = run.ContentStart.GetPositionAtOffset(start, System.Windows.Documents.LogicalDirection.Forward)
            ?? throw new InvalidOperationException($"Could not create selection start at offset {start}.");
        var selectionEnd = run.ContentStart.GetPositionAtOffset(start + length, System.Windows.Documents.LogicalDirection.Backward)
            ?? throw new InvalidOperationException($"Could not create selection end at offset {start + length}.");
        box.Selection.Select(selectionStart, selectionEnd);
    }

    static void InvokeTextEditorTyping(string methodName, params object?[] args)
    {
        var typingType = typeof(WpfRichTextBox).Assembly.GetType("System.Windows.Documents.TextEditorTyping")
            ?? throw new InvalidOperationException("TextEditorTyping type not found.");
        var method = typingType.GetMethod(
            methodName,
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)
            ?? throw new InvalidOperationException($"TextEditorTyping.{methodName} not found.");
        method.Invoke(null, args);
    }

    static void InvokeTextEditorCharacters(string methodName, params object?[] args)
    {
        var charactersType = typeof(WpfRichTextBox).Assembly.GetType("System.Windows.Documents.TextEditorCharacters")
            ?? throw new InvalidOperationException("TextEditorCharacters type not found.");
        var method = charactersType.GetMethod(
            methodName,
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)
            ?? throw new InvalidOperationException($"TextEditorCharacters.{methodName} not found.");
        method.Invoke(null, args);
    }

    static void InvokeTextEditorParagraphs(string methodName, params object?[] args)
    {
        var paragraphsType = typeof(WpfRichTextBox).Assembly.GetType("System.Windows.Documents.TextEditorParagraphs")
            ?? throw new InvalidOperationException("TextEditorParagraphs type not found.");
        var method = paragraphsType.GetMethod(
            methodName,
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)
            ?? throw new InvalidOperationException($"TextEditorParagraphs.{methodName} not found.");
        method.Invoke(null, args);
    }

    static void InvokeRichTextBoxOnKeyDown(
        WpfRichTextBox box,
        global::Windows.System.VirtualKey key,
        global::Windows.System.VirtualKeyModifiers modifiers = global::Windows.System.VirtualKeyModifiers.None)
    {
        var ctor = typeof(Microsoft.UI.Xaml.Input.KeyRoutedEventArgs).GetConstructor(
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic,
            binder: null,
            types:
            [
                typeof(object),
                typeof(global::Windows.System.VirtualKey),
                typeof(global::Windows.System.VirtualKeyModifiers),
                typeof(global::Windows.UI.Core.CorePhysicalKeyStatus?),
                typeof(char?),
            ],
            modifiers: null)
            ?? throw new InvalidOperationException("KeyRoutedEventArgs constructor not found.");
        var args = ctor.Invoke(
        [
            box,
            key,
            modifiers,
            null,
            null,
        ]);
        var method = typeof(WpfRichTextBox).GetMethod(
            "OnKeyDown",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            binder: null,
            types: [typeof(Microsoft.UI.Xaml.Input.KeyRoutedEventArgs)],
            modifiers: null)
            ?? throw new InvalidOperationException("RichTextBox.OnKeyDown not found.");
        var modifiersProperty = typeof(System.Windows.Input.Keyboard).GetProperty(
            "ModifiersOverride",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        var previousModifiers = modifiersProperty?.GetValue(null);
        try
        {
            modifiersProperty?.SetValue(null, ToWpfModifiers(modifiers));
            method.Invoke(box, new object[] { args });
        }
        finally
        {
            modifiersProperty?.SetValue(null, previousModifiers);
        }
    }

    static global::Windows.System.VirtualKey ParseVirtualKey(string key) =>
        Enum.TryParse<global::Windows.System.VirtualKey>(key, ignoreCase: true, out var parsed)
            ? parsed
            : throw new ArgumentException($"Unsupported key '{key}'.", nameof(key));

    static global::Windows.System.VirtualKeyModifiers ParseVirtualKeyModifiers(string modifiers)
    {
        var result = global::Windows.System.VirtualKeyModifiers.None;
        foreach (var part in modifiers.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!Enum.TryParse<global::Windows.System.VirtualKeyModifiers>(part, ignoreCase: true, out var parsed))
                throw new ArgumentException($"Unsupported modifier '{part}'.", nameof(modifiers));
            result |= parsed;
        }

        return result;
    }

    static System.Windows.Input.ModifierKeys ToWpfModifiers(global::Windows.System.VirtualKeyModifiers modifiers)
    {
        var result = System.Windows.Input.ModifierKeys.None;
        if ((modifiers & global::Windows.System.VirtualKeyModifiers.Control) != 0)
            result |= System.Windows.Input.ModifierKeys.Control;
        if ((modifiers & global::Windows.System.VirtualKeyModifiers.Shift) != 0)
            result |= System.Windows.Input.ModifierKeys.Shift;
        if ((modifiers & global::Windows.System.VirtualKeyModifiers.Menu) != 0)
            result |= System.Windows.Input.ModifierKeys.Alt;
        return result;
    }

    [DevFlowAction("richtextbox.probe.state", Description = "RichTextBox state snapshot as JSON.")]
    public static string ProbeState() => RunOnUi(Snapshot);

    [DevFlowAction("richtextbox.probe.create-plain", Description = "Create a RichTextBox and append plain text.")]
    public static string ProbeCreatePlain(string text) => RunOnUi(page =>
    {
        var box = RichTextBoxScenarios.BuildPlainTextBox(text);
        page._root.Children.Clear();
        page._box = box;
        page._root.Children.Add(box);
        box.ApplyTemplate();
        box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.set-document", Description = "Create a RichTextBox with a FlowDocument containing one paragraph/run.")]
    public static string ProbeSetDocument(string text) => RunOnUi(page =>
    {
        var box = new WpfRichTextBox
        {
            Width = 640,
            Height = 240,
            AcceptsReturn = true,
            Document = RichTextBoxScenarios.BuildSimpleDocument(text),
        };
        page._root.Children.Clear();
        page._box = box;
        page._root.Children.Add(box);
        box.ApplyTemplate();
        box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.append", Description = "Append text to the current RichTextBox and read back the document text.")]
    public static string ProbeAppend(string text) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.AppendText(text);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.text-input", Description = "Drive the TextEditorTyping text input path for the current RichTextBox.")]
    public static string ProbeTextInput(string text) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        var textEditor = RequireTextEditor(page._box);
        InvokeTextEditorTyping("DoTextInput", textEditor, text, false, false);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.text-input-event", Description = "Drive TextEditorTyping.OnTextInput for the current RichTextBox.")]
    public static string ProbeTextInputEvent(string text) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        foreach (var c in text)
        {
            if (char.IsControl(c))
                continue;

            var args = new WpfTextCompositionEventArgs(c.ToString())
            {
                OriginalSource = page._box,
            };
            InvokeTextEditorTyping("OnTextInput", page._box, args);
        }

        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.replace-selection-text-input-event", Description = "Select all text and drive TextEditorTyping.OnTextInput to replace it.")]
    public static string ProbeReplaceSelectionTextInputEvent(string text) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        page._box.SelectAll();
        foreach (var c in text)
        {
            if (char.IsControl(c))
                continue;

            var args = new WpfTextCompositionEventArgs(c.ToString())
            {
                OriginalSource = page._box,
            };
            InvokeTextEditorTyping("OnTextInput", page._box, args);
        }

        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.backspace-command", Description = "Invoke TextEditorTyping's Backspace command handler for the current RichTextBox.")]
    public static string ProbeBackspaceCommand() => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        var args = new WpfExecutedRoutedEventArgs(WpfEditingCommands.Backspace, null)
        {
            Source = page._box,
            OriginalSource = page._box,
        };
        InvokeTextEditorTyping("OnBackspace", page._box, args);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.delete-selection-command", Description = "Select all text and invoke TextEditorTyping's Delete command handler for the current RichTextBox.")]
    public static string ProbeDeleteSelectionCommand() => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        page._box.SelectAll();
        var args = new WpfExecutedRoutedEventArgs(WpfEditingCommands.Delete, null)
        {
            Source = page._box,
            OriginalSource = page._box,
        };
        InvokeTextEditorTyping("OnDelete", page._box, args);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.undo", Description = "Invoke RichTextBox.Undo and read back document text and undo state.")]
    public static string ProbeUndo() => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        page._box.Undo();
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.redo", Description = "Invoke RichTextBox.Redo and read back document text and undo state.")]
    public static string ProbeRedo() => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        page._box.Redo();
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.toggle-bold-selection-command", Description = "Select all text and invoke TextEditorCharacters' ToggleBold command handler for the current RichTextBox.")]
    public static string ProbeToggleBoldSelectionCommand() => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        page._box.SelectAll();
        var args = new WpfExecutedRoutedEventArgs(WpfDocumentEditingCommands.ToggleBold, null)
        {
            Source = page._box,
            OriginalSource = page._box,
        };
        InvokeTextEditorCharacters("OnToggleBold", page._box, args);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.toggle-bold-run-range-command", Description = "Select a range inside the first Run and invoke TextEditorCharacters' ToggleBold command handler for the current RichTextBox.")]
    public static string ProbeToggleBoldRunRangeCommand(int start, int length) =>
        ProbeToggleRunRangeCommand("OnToggleBold", WpfDocumentEditingCommands.ToggleBold, start, length);

    [DevFlowAction("richtextbox.probe.toggle-italic-run-range-command", Description = "Select a range inside the first Run and invoke TextEditorCharacters' ToggleItalic command handler for the current RichTextBox.")]
    public static string ProbeToggleItalicRunRangeCommand(int start, int length) =>
        ProbeToggleRunRangeCommand("OnToggleItalic", WpfDocumentEditingCommands.ToggleItalic, start, length);

    [DevFlowAction("richtextbox.probe.toggle-underline-run-range-command", Description = "Select a range inside the first Run and invoke TextEditorCharacters' ToggleUnderline command handler for the current RichTextBox.")]
    public static string ProbeToggleUnderlineRunRangeCommand(int start, int length) =>
        ProbeToggleRunRangeCommand("OnToggleUnderline", WpfDocumentEditingCommands.ToggleUnderline, start, length);

    static string ProbeToggleRunRangeCommand(string methodName, System.Windows.Input.ICommand command, int start, int length) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        SelectFirstRunTextRange(page._box, start, length);
        var args = new WpfExecutedRoutedEventArgs(command, null)
        {
            Source = page._box,
            OriginalSource = page._box,
        };
        InvokeTextEditorCharacters(methodName, page._box, args);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.toggle-italic-selection-command", Description = "Select all text and invoke TextEditorCharacters' ToggleItalic command handler for the current RichTextBox.")]
    public static string ProbeToggleItalicSelectionCommand() => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        page._box.SelectAll();
        var args = new WpfExecutedRoutedEventArgs(WpfDocumentEditingCommands.ToggleItalic, null)
        {
            Source = page._box,
            OriginalSource = page._box,
        };
        InvokeTextEditorCharacters("OnToggleItalic", page._box, args);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.toggle-underline-selection-command", Description = "Select all text and invoke TextEditorCharacters' ToggleUnderline command handler for the current RichTextBox.")]
    public static string ProbeToggleUnderlineSelectionCommand() => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        page._box.SelectAll();
        var args = new WpfExecutedRoutedEventArgs(WpfDocumentEditingCommands.ToggleUnderline, null)
        {
            Source = page._box,
            OriginalSource = page._box,
        };
        InvokeTextEditorCharacters("OnToggleUnderline", page._box, args);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.apply-font-size-selection-command", Description = "Select all text and invoke TextEditorCharacters' ApplyFontSize command handler for the current RichTextBox.")]
    public static string ProbeApplyFontSizeSelectionCommand(double fontSize) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        page._box.SelectAll();
        var args = new WpfExecutedRoutedEventArgs(WpfDocumentEditingCommands.ToggleBold, fontSize)
        {
            Source = page._box,
            OriginalSource = page._box,
        };
        InvokeTextEditorCharacters("OnApplyFontSize", page._box, args);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.increase-font-size-selection-command", Description = "Select all text and invoke TextEditorCharacters' IncreaseFontSize command handler for the current RichTextBox.")]
    public static string ProbeIncreaseFontSizeSelectionCommand() => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        page._box.SelectAll();
        var args = new WpfExecutedRoutedEventArgs(WpfDocumentEditingCommands.ToggleBold, null)
        {
            Source = page._box,
            OriginalSource = page._box,
        };
        InvokeTextEditorCharacters("OnIncreaseFontSize", page._box, args);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.decrease-font-size-selection-command", Description = "Select all text and invoke TextEditorCharacters' DecreaseFontSize command handler for the current RichTextBox.")]
    public static string ProbeDecreaseFontSizeSelectionCommand() => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        page._box.SelectAll();
        var args = new WpfExecutedRoutedEventArgs(WpfDocumentEditingCommands.ToggleBold, null)
        {
            Source = page._box,
            OriginalSource = page._box,
        };
        InvokeTextEditorCharacters("OnDecreaseFontSize", page._box, args);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.apply-font-family-selection-command", Description = "Select all text and invoke TextEditorCharacters' ApplyFontFamily command handler for the current RichTextBox.")]
    public static string ProbeApplyFontFamilySelectionCommand(string fontFamily) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        page._box.SelectAll();
        var args = new WpfExecutedRoutedEventArgs(WpfDocumentEditingCommands.ToggleBold, new Microsoft.UI.Xaml.Media.FontFamily(fontFamily))
        {
            Source = page._box,
            OriginalSource = page._box,
        };
        InvokeTextEditorCharacters("OnApplyFontFamily", page._box, args);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.apply-foreground-selection-command", Description = "Select all text and invoke TextEditorCharacters' ApplyForeground command handler for the current RichTextBox.")]
    public static string ProbeApplyForegroundSelectionCommand() => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        page._box.SelectAll();
        var args = new WpfExecutedRoutedEventArgs(WpfDocumentEditingCommands.ToggleBold, System.Windows.Media.Brushes.LightGreen)
        {
            Source = page._box,
            OriginalSource = page._box,
        };
        InvokeTextEditorCharacters("OnApplyForeground", page._box, args);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.apply-background-selection-command", Description = "Select all text and invoke TextEditorCharacters' ApplyBackground command handler for the current RichTextBox.")]
    public static string ProbeApplyBackgroundSelectionCommand() => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        page._box.SelectAll();
        var args = new WpfExecutedRoutedEventArgs(WpfDocumentEditingCommands.ToggleBold, System.Windows.Media.Brushes.LightPink)
        {
            Source = page._box,
            OriginalSource = page._box,
        };
        InvokeTextEditorCharacters("OnApplyBackground", page._box, args);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    static string ProbeAlignSelectionCommand(string methodName, System.Windows.Input.ICommand command) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        page._box.SelectAll();
        var args = new WpfExecutedRoutedEventArgs(command, null)
        {
            Source = page._box,
            OriginalSource = page._box,
        };
        InvokeTextEditorParagraphs(methodName, page._box, args);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.align-left-selection-command", Description = "Select all text and invoke TextEditorParagraphs' AlignLeft command handler for the current RichTextBox.")]
    public static string ProbeAlignLeftSelectionCommand() =>
        ProbeAlignSelectionCommand("OnAlignLeft", WpfDocumentEditingCommands.AlignLeft);

    [DevFlowAction("richtextbox.probe.align-center-selection-command", Description = "Select all text and invoke TextEditorParagraphs' AlignCenter command handler for the current RichTextBox.")]
    public static string ProbeAlignCenterSelectionCommand() =>
        ProbeAlignSelectionCommand("OnAlignCenter", WpfDocumentEditingCommands.AlignCenter);

    [DevFlowAction("richtextbox.probe.align-right-selection-command", Description = "Select all text and invoke TextEditorParagraphs' AlignRight command handler for the current RichTextBox.")]
    public static string ProbeAlignRightSelectionCommand() =>
        ProbeAlignSelectionCommand("OnAlignRight", WpfDocumentEditingCommands.AlignRight);

    [DevFlowAction("richtextbox.probe.align-justify-selection-command", Description = "Select all text and invoke TextEditorParagraphs' AlignJustify command handler for the current RichTextBox.")]
    public static string ProbeAlignJustifySelectionCommand() =>
        ProbeAlignSelectionCommand("OnAlignJustify", WpfDocumentEditingCommands.AlignJustify);

    [DevFlowAction("richtextbox.probe.apply-single-space-selection-command", Description = "Select all text and invoke TextEditorParagraphs' ApplySingleSpace command handler for the current RichTextBox.")]
    public static string ProbeApplySingleSpaceSelectionCommand() =>
        ProbeAlignSelectionCommand("OnApplySingleSpace", WpfDocumentEditingCommands.AlignLeft);

    [DevFlowAction("richtextbox.probe.apply-one-and-a-half-space-selection-command", Description = "Select all text and invoke TextEditorParagraphs' ApplyOneAndAHalfSpace command handler for the current RichTextBox.")]
    public static string ProbeApplyOneAndAHalfSpaceSelectionCommand() =>
        ProbeAlignSelectionCommand("OnApplyOneAndAHalfSpace", WpfDocumentEditingCommands.AlignLeft);

    [DevFlowAction("richtextbox.probe.apply-double-space-selection-command", Description = "Select all text and invoke TextEditorParagraphs' ApplyDoubleSpace command handler for the current RichTextBox.")]
    public static string ProbeApplyDoubleSpaceSelectionCommand() =>
        ProbeAlignSelectionCommand("OnApplyDoubleSpace", WpfDocumentEditingCommands.AlignLeft);

    [DevFlowAction("richtextbox.probe.apply-paragraph-flow-direction-ltr-selection-command", Description = "Select all text and invoke TextEditorParagraphs' ApplyParagraphFlowDirectionLTR command handler for the current RichTextBox.")]
    public static string ProbeApplyParagraphFlowDirectionLtrSelectionCommand() =>
        ProbeAlignSelectionCommand("OnApplyParagraphFlowDirectionLTR", WpfDocumentEditingCommands.AlignLeft);

    [DevFlowAction("richtextbox.probe.apply-paragraph-flow-direction-rtl-selection-command", Description = "Select all text and invoke TextEditorParagraphs' ApplyParagraphFlowDirectionRTL command handler for the current RichTextBox.")]
    public static string ProbeApplyParagraphFlowDirectionRtlSelectionCommand() =>
        ProbeAlignSelectionCommand("OnApplyParagraphFlowDirectionRTL", WpfDocumentEditingCommands.AlignLeft);

    [DevFlowAction("richtextbox.probe.key-down", Description = "Invoke RichTextBox.OnKeyDown with a Uno KeyRoutedEventArgs for the current RichTextBox.")]
    public static string ProbeKeyDown(string key) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        InvokeRichTextBoxOnKeyDown(page._box, ParseVirtualKey(key));
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.key-down-select-all", Description = "Select all text and invoke RichTextBox.OnKeyDown with a Uno KeyRoutedEventArgs for the current RichTextBox.")]
    public static string ProbeKeyDownSelectAll(string key) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        page._box.SelectAll();
        InvokeRichTextBoxOnKeyDown(page._box, ParseVirtualKey(key));
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.key-down-select-all-modifiers", Description = "Select all text and invoke RichTextBox.OnKeyDown with a Uno KeyRoutedEventArgs and modifiers.")]
    public static string ProbeKeyDownSelectAllModifiers(string key, string modifiers) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        page._box.SelectAll();
        InvokeRichTextBoxOnKeyDown(page._box, ParseVirtualKey(key), ParseVirtualKeyModifiers(modifiers));
        page._box.UpdateLayout();
        return Snapshot(page);
    });
}
#else
public sealed partial class MainPage : Microsoft.UI.Xaml.Controls.Page
{
}
#endif
