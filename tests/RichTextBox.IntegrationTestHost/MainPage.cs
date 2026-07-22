#if DEBUG
using System.Threading;
using LeXtudio.DevFlow.Agent.Core;
using Microsoft.UI.Xaml.Controls;
using RichTextBox.TestScenarios;
using WpfDocumentEditingCommands = System.Windows.Documents.EditingCommands;
using WpfExecutedRoutedEventArgs = System.Windows.Input.ExecutedRoutedEventArgs;
using WpfEditingCommands = System.Windows.Input.EditingCommands;
using WpfInline = System.Windows.Documents.Inline;
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
        var selectionText = selection?.Text;
        var selectionFontWeight = selection is null
            ? null
            : FormatFontWeight(selection.GetPropertyValue(WpfTextElement.FontWeightProperty));
        var firstInline = document?.Blocks.FirstBlock is System.Windows.Documents.Paragraph paragraph
            ? paragraph.Inlines.FirstInline
            : null;
        var firstInlineFontWeight = firstInline is null
            ? null
            : FormatFontWeight(firstInline.GetValue(WpfTextElement.FontWeightProperty));
        var firstInlineFontStyle = firstInline is null
            ? null
            : firstInline.GetValue(WpfTextElement.FontStyleProperty)?.ToString();
        var firstInlineHasUnderline = firstInline is not null
            && HasUnderline(firstInline.GetValue(WpfInline.TextDecorationsProperty));

        return $"{{\"hasRichTextBox\":{Jb(box is not null)},\"hasDocument\":{Jb(document is not null)},\"blockCount\":{(document?.Blocks.Count ?? 0)},\"text\":{Js(text)},\"selectionText\":{Js(selectionText)},\"selectionFontWeight\":{Js(selectionFontWeight)},\"firstInlineType\":{Js(firstInline?.GetType().FullName)},\"firstInlineFontWeight\":{Js(firstInlineFontWeight)},\"firstInlineFontStyle\":{Js(firstInlineFontStyle)},\"firstInlineHasUnderline\":{Jb(firstInlineHasUnderline)},\"contentHostAvailable\":{Jb(contentHostAvailable)},\"renderScopeType\":{Js(renderScope?.GetType().FullName)},\"textViewType\":{Js(textView?.GetType().FullName)}}}";
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

    static void InvokeRichTextBoxOnKeyDown(WpfRichTextBox box, global::Windows.System.VirtualKey key)
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
            global::Windows.System.VirtualKeyModifiers.None,
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
        method.Invoke(box, new object[] { args });
    }

    static global::Windows.System.VirtualKey ParseVirtualKey(string key) =>
        Enum.TryParse<global::Windows.System.VirtualKey>(key, ignoreCase: true, out var parsed)
            ? parsed
            : throw new ArgumentException($"Unsupported key '{key}'.", nameof(key));

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
}
#else
public sealed partial class MainPage : Microsoft.UI.Xaml.Controls.Page
{
}
#endif
