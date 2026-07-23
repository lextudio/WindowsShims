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
using LeXtudio.UI.Text.Core;
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

    static string DescribeBlockTypes(System.Windows.Documents.BlockCollection blocks)
    {
        var parts = new List<string>();
        var block = blocks.FirstBlock;
        while (block is not null)
        {
            parts.Add(block.GetType().Name);
            block = block.NextBlock;
        }
        return string.Join(",", parts);
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
                var flowDirection = run.GetValue(WpfInline.FlowDirectionProperty)?.ToString();
                parts.Add($"Run:{InlineText(run.Text)}:w={FormatFontWeight(run.GetValue(WpfTextElement.FontWeightProperty))}:s={style}:z={size}:d={underline}:fd={flowDirection}");
            }
            else if (inline is WpfSpan span)
            {
                var style = span.GetValue(WpfTextElement.FontStyleProperty)?.ToString();
                var size = span.GetValue(WpfTextElement.FontSizeProperty)?.ToString();
                var underline = HasUnderline(span.GetValue(WpfInline.TextDecorationsProperty)) ? "U" : "-";
                var flowDirection = span.GetValue(WpfInline.FlowDirectionProperty)?.ToString();
                parts.Add($"{span.GetType().Name}:w={FormatFontWeight(span.GetValue(WpfTextElement.FontWeightProperty))}:s={style}:z={size}:d={underline}:fd={flowDirection}");
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
        var clipboardText = System.Windows.Clipboard.GetText();
        var firstParagraph = document?.Blocks.FirstBlock as WpfParagraph;
        var firstInline = firstParagraph is not null
            ? firstParagraph.Inlines.FirstInline
            : null;
        var firstRun = FirstRun(firstInline);
        var selectionStartRunOffset = firstRun is null || selection is null
            ? (int?)null
            : firstRun.ContentStart.GetOffsetToPosition(selection.Start);
        var selectionEndRunOffset = firstRun is null || selection is null
            ? (int?)null
            : firstRun.ContentStart.GetOffsetToPosition(selection.End);
        var firstBlockType = document?.Blocks.FirstBlock?.GetType().Name;
        var firstList = document?.Blocks.FirstBlock as System.Windows.Documents.List;
        var firstListMarkerStyle = firstList?.MarkerStyle.ToString();
        var firstListItemCount = firstList?.ListItems.Count;
        var firstListItemText = firstList?.ListItems.FirstListItem?.Blocks.FirstBlock is WpfParagraph listItemParagraph
            ? new WpfTextRange(listItemParagraph.ContentStart, listItemParagraph.ContentEnd).Text
            : null;
        var firstListItemBlockTypes = firstList?.ListItems.FirstListItem is { } firstListItemForBlocks
            ? DescribeBlockTypes(firstListItemForBlocks.Blocks)
            : null;
        var nestedListMarkerStyle = firstList?.ListItems.FirstListItem?.Blocks.FirstBlock is System.Windows.Documents.List nestedListAsFirstBlock
            ? nestedListAsFirstBlock.MarkerStyle.ToString()
            : firstList?.ListItems.FirstListItem?.Blocks.LastBlock is System.Windows.Documents.List nestedListAsLastBlock
                ? nestedListAsLastBlock.MarkerStyle.ToString()
                : null;
        var nestedListItemCount = firstList?.ListItems.FirstListItem?.Blocks.FirstBlock is System.Windows.Documents.List nestedListCountFirst
            ? nestedListCountFirst.ListItems.Count
            : firstList?.ListItems.FirstListItem?.Blocks.LastBlock is System.Windows.Documents.List nestedListCountLast
                ? nestedListCountLast.ListItems.Count
                : (int?)null;
        var firstParagraphTextAlignment = firstParagraph?.TextAlignment.ToString();
        var firstParagraphLineHeight = firstParagraph?.LineHeight.ToString();
        var firstParagraphLineStackingStrategy = firstParagraph?.LineStackingStrategy.ToString();
        var firstParagraphFlowDirection = firstParagraph?.FlowDirection.ToString();
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
        var firstInlineFlowDirection = firstInline is null
            ? null
            : firstInline.GetValue(WpfInline.FlowDirectionProperty)?.ToString();
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
        var firstRunFlowDirection = firstRun is null
            ? null
            : firstRun.GetValue(WpfInline.FlowDirectionProperty)?.ToString();
        var firstRunHasUnderline = firstRun is not null
            && HasUnderline(firstRun.GetValue(WpfInline.TextDecorationsProperty));

        return $"{{\"hasRichTextBox\":{Jb(box is not null)},\"hasDocument\":{Jb(document is not null)},\"blockCount\":{(document?.Blocks.Count ?? 0)},\"text\":{Js(text)},\"canUndo\":{Jb(canUndo)},\"canRedo\":{Jb(canRedo)},\"selectionText\":{Js(selectionText)},\"selectionFontWeight\":{Js(selectionFontWeight)},\"selectionStartRunOffset\":{(selectionStartRunOffset?.ToString() ?? "null")},\"selectionEndRunOffset\":{(selectionEndRunOffset?.ToString() ?? "null")},\"clipboardText\":{Js(clipboardText)},\"firstBlockType\":{Js(firstBlockType)},\"firstListMarkerStyle\":{Js(firstListMarkerStyle)},\"firstListItemCount\":{(firstListItemCount?.ToString() ?? "null")},\"firstListItemText\":{Js(firstListItemText)},\"firstListItemBlockTypes\":{Js(firstListItemBlockTypes)},\"nestedListMarkerStyle\":{Js(nestedListMarkerStyle)},\"nestedListItemCount\":{(nestedListItemCount?.ToString() ?? "null")},\"firstParagraphTextAlignment\":{Js(firstParagraphTextAlignment)},\"firstParagraphLineHeight\":{Js(firstParagraphLineHeight)},\"firstParagraphLineStackingStrategy\":{Js(firstParagraphLineStackingStrategy)},\"firstParagraphFlowDirection\":{Js(firstParagraphFlowDirection)},\"inlineTree\":{Js(inlineTree)},\"firstInlineType\":{Js(firstInline?.GetType().FullName)},\"firstInlineFontWeight\":{Js(firstInlineFontWeight)},\"firstInlineFontStyle\":{Js(firstInlineFontStyle)},\"firstInlineFontSize\":{Js(firstInlineFontSize)},\"firstInlineFontFamily\":{Js(firstInlineFontFamily)},\"firstInlineForeground\":{Js(firstInlineForeground)},\"firstInlineBackground\":{Js(firstInlineBackground)},\"firstInlineFlowDirection\":{Js(firstInlineFlowDirection)},\"firstInlineHasUnderline\":{Jb(firstInlineHasUnderline)},\"firstRunFontWeight\":{Js(firstRunFontWeight)},\"firstRunFontStyle\":{Js(firstRunFontStyle)},\"firstRunFontSize\":{Js(firstRunFontSize)},\"firstRunFontFamily\":{Js(firstRunFontFamily)},\"firstRunForeground\":{Js(firstRunForeground)},\"firstRunBackground\":{Js(firstRunBackground)},\"firstRunFlowDirection\":{Js(firstRunFlowDirection)},\"firstRunHasUnderline\":{Jb(firstRunHasUnderline)},\"contentHostAvailable\":{Jb(contentHostAvailable)},\"renderScopeType\":{Js(renderScope?.GetType().FullName)},\"textViewType\":{Js(textView?.GetType().FullName)}}}";
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

    static void InvokeTextEditorListsOnListCommand(WpfRichTextBox box, System.Windows.Input.RoutedUICommand command)
    {
        var listsType = typeof(WpfRichTextBox).Assembly.GetType("System.Windows.Documents.TextEditorLists")
            ?? throw new InvalidOperationException("TextEditorLists type not found.");
        var method = listsType.GetMethod(
            "OnListCommand",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)
            ?? throw new InvalidOperationException("TextEditorLists.OnListCommand not found.");
        var args = new WpfExecutedRoutedEventArgs(command, null)
        {
            Source = box,
            OriginalSource = box,
        };
        method.Invoke(null, [box, args]);
    }

    static void InvokeRichTextBoxOnKeyDown(
        WpfRichTextBox box,
        global::Windows.System.VirtualKey key,
        global::Windows.System.VirtualKeyModifiers modifiers = global::Windows.System.VirtualKeyModifiers.None)
        => InvokeRichTextBoxKeyMethod(box, "OnKeyDown", key, modifiers);

    static void InvokeRichTextBoxOnKeyUp(
        WpfRichTextBox box,
        global::Windows.System.VirtualKey key,
        global::Windows.System.VirtualKeyModifiers modifiers = global::Windows.System.VirtualKeyModifiers.None)
        => InvokeRichTextBoxKeyMethod(box, "OnKeyUp", key, modifiers);

    static void InvokeRichTextBoxKeyMethod(
        WpfRichTextBox box,
        string methodName,
        global::Windows.System.VirtualKey key,
        global::Windows.System.VirtualKeyModifiers modifiers)
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
            methodName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            binder: null,
            types: [typeof(Microsoft.UI.Xaml.Input.KeyRoutedEventArgs)],
            modifiers: null)
            ?? throw new InvalidOperationException($"RichTextBox.{methodName} not found.");
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

    [DevFlowAction("richtextbox.probe.set-list-document", Description = "Create a RichTextBox with a FlowDocument containing a two-item List built directly (bypassing List.Apply).")]
    public static string ProbeSetListDocument(string firstItemText, string secondItemText) => RunOnUi(page =>
    {
        var box = new WpfRichTextBox
        {
            Width = 640,
            Height = 240,
            AcceptsReturn = true,
            Document = RichTextBoxScenarios.BuildListDocument(firstItemText, secondItemText),
        };
        page._root.Children.Clear();
        page._box = box;
        page._root.Children.Add(box);
        box.ApplyTemplate();
        box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.set-numbered-list-document", Description = "Create a RichTextBox with a FlowDocument containing a two-item Decimal-marker List built directly (bypassing List.Apply).")]
    public static string ProbeSetNumberedListDocument(string firstItemText, string secondItemText) => RunOnUi(page =>
    {
        var box = new WpfRichTextBox
        {
            Width = 640,
            Height = 240,
            AcceptsReturn = true,
            Document = RichTextBoxScenarios.BuildListDocument(System.Windows.TextMarkerStyle.Decimal, firstItemText, secondItemText),
        };
        page._root.Children.Clear();
        page._box = box;
        page._root.Children.Add(box);
        box.ApplyTemplate();
        box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.set-table-document", Description = "Create a RichTextBox with a FlowDocument containing a 2x2 Table built directly via constructors.")]
    public static string ProbeSetTableDocument(string cell00, string cell01, string cell10, string cell11) => RunOnUi(page =>
    {
        var box = new WpfRichTextBox
        {
            Width = 640,
            Height = 240,
            AcceptsReturn = true,
            Document = RichTextBoxScenarios.BuildTableDocument(cell00, cell01, cell10, cell11),
        };
        page._root.Children.Clear();
        page._box = box;
        page._root.Children.Add(box);
        box.ApplyTemplate();
        box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.set-hyperlink-document", Description = "Create a RichTextBox with a FlowDocument containing before/hyperlink/after Runs in one Paragraph.")]
    public static string ProbeSetHyperlinkDocument(string beforeText, string linkText, string afterText) => RunOnUi(page =>
    {
        var box = new WpfRichTextBox
        {
            Width = 640,
            Height = 240,
            AcceptsReturn = true,
            Document = RichTextBoxScenarios.BuildHyperlinkDocument(beforeText, linkText, afterText, new Uri("https://example.invalid/")),
        };
        page._root.Children.Clear();
        page._box = box;
        page._root.Children.Add(box);
        box.ApplyTemplate();
        box.UpdateLayout();
        return Snapshot(page);
    });

    static object RequireRenderScope(WpfRichTextBox box) =>
        GetInternalProperty(box, "RenderScope")
            ?? throw new InvalidOperationException("RichTextBox.RenderScope is not available.");

    [DevFlowAction("richtextbox.probe.get-hyperlink-rect", Description = "Reflect into the rendered page layout to find the hyperlink run's rect (x, y, width, height).")]
    public static string ProbeGetHyperlinkRect() => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.set-hyperlink-document first.");

        var renderScope = RequireRenderScope(page._box);
        var pageLayout = GetInternalProperty(renderScope, "Page")
            ?? throw new InvalidOperationException("FlowDocumentView.Page is not available (layout not run yet?).");
        var lines = (System.Collections.IEnumerable)(GetInternalProperty(pageLayout, "Lines")
            ?? throw new InvalidOperationException("FlorencePage.Lines not found."));

        foreach (var line in lines)
        {
            var lineY = (double)GetInternalProperty(line, "Y")!;
            var lineHeight = (double)GetInternalProperty(line, "Height")!;
            var runs = (System.Collections.IEnumerable)GetInternalProperty(line, "Runs")!;
            foreach (var run in runs)
            {
                var hyperlink = GetInternalProperty(run, "Hyperlink");
                if (hyperlink is null)
                    continue;

                var runX = (double)GetInternalProperty(run, "X")!;
                var runWidth = (double)GetInternalProperty(run, "Width")!;
                return $"{{\"found\":true,\"x\":{runX.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"y\":{lineY.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"width\":{runWidth.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"height\":{lineHeight.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}";
            }
        }

        return "{\"found\":false}";
    });

    [DevFlowAction("richtextbox.probe.hyperlink-hit-test", Description = "Call FlowDocumentView.GetHyperlinkAt at the given point and report whether a Hyperlink was found.")]
    public static string ProbeHyperlinkHitTest(double x, double y) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.set-hyperlink-document first.");

        var renderScope = RequireRenderScope(page._box);
        var method = renderScope.GetType().GetMethod("GetHyperlinkAt", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("FlowDocumentView.GetHyperlinkAt not found.");
        var point = new Windows.Foundation.Point(x, y);
        var hyperlink = method.Invoke(renderScope, [point]) as System.Windows.Documents.Hyperlink;
        var linkText = hyperlink is null
            ? null
            : new WpfTextRange(hyperlink.ContentStart, hyperlink.ContentEnd).Text;
        return $"{{\"hyperlinkFound\":{Jb(hyperlink is not null)},\"linkText\":{Js(linkText)}}}";
    });

    [DevFlowAction("richtextbox.probe.raise-hyperlink-click-at", Description = "Hit-test for a Hyperlink at the given point and, if found, raise its Click event directly (does NOT call ActivateHyperlink / launch the NavigateUri — CI-safe).")]
    public static string ProbeRaiseHyperlinkClickAt(double x, double y) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.set-hyperlink-document first.");

        var renderScope = RequireRenderScope(page._box);
        var hitTestMethod = renderScope.GetType().GetMethod("GetHyperlinkAt", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("FlowDocumentView.GetHyperlinkAt not found.");
        var point = new Windows.Foundation.Point(x, y);
        var hyperlink = hitTestMethod.Invoke(renderScope, [point]) as System.Windows.Documents.Hyperlink;
        if (hyperlink is null)
            return "{\"hyperlinkFound\":false,\"clickRaised\":false}";

        var clickRaised = false;
        System.Windows.RoutedEventHandler handler = (_, _) => clickRaised = true;
        hyperlink.Click += handler;
        try
        {
            var raiseClickMethod = typeof(System.Windows.Documents.Hyperlink).GetMethod("RaiseClick", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Hyperlink.RaiseClick not found.");
            raiseClickMethod.Invoke(hyperlink, null);
        }
        finally
        {
            hyperlink.Click -= handler;
        }

        return $"{{\"hyperlinkFound\":true,\"clickRaised\":{Jb(clickRaised)}}}";
    });

    [DevFlowAction("richtextbox.probe.activate-hyperlink-at", Description = "Hit-test for a Hyperlink at the given point and, if found, activate it, reporting whether Click fired.")]
    public static string ProbeActivateHyperlinkAt(double x, double y) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.set-hyperlink-document first.");

        var renderScope = RequireRenderScope(page._box);
        var hitTestMethod = renderScope.GetType().GetMethod("GetHyperlinkAt", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("FlowDocumentView.GetHyperlinkAt not found.");
        var point = new Windows.Foundation.Point(x, y);
        var hyperlink = hitTestMethod.Invoke(renderScope, [point]) as System.Windows.Documents.Hyperlink;
        if (hyperlink is null)
            return "{\"hyperlinkFound\":false,\"clickRaised\":false}";

        var clickRaised = false;
        System.Windows.RoutedEventHandler handler = (_, _) => clickRaised = true;
        hyperlink.Click += handler;
        try
        {
            var activateMethod = renderScope.GetType().GetMethod("ActivateHyperlink", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("FlowDocumentView.ActivateHyperlink not found.");
            activateMethod.Invoke(renderScope, [hyperlink]);
        }
        finally
        {
            hyperlink.Click -= handler;
        }

        return $"{{\"hyperlinkFound\":true,\"clickRaised\":{Jb(clickRaised)}}}";
    });

    [DevFlowAction("richtextbox.probe.caret-hit-test-round-trip", Description = "Compute the character rect at an offset in the first Run, hit-test its center, and report the resulting CharOffset.")]
    public static string ProbeCaretHitTestRoundTrip(int offset) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        var document = page._box.Document ?? throw new InvalidOperationException("RichTextBox has no Document.");
        var paragraph = document.Blocks.FirstBlock as WpfParagraph ?? throw new InvalidOperationException("First block is not a Paragraph.");
        var run = FirstRun(paragraph.Inlines.FirstInline) ?? throw new InvalidOperationException("First Paragraph does not contain a plain Run.");

        var position = run.ContentStart.GetPositionAtOffset(offset)
            ?? throw new InvalidOperationException($"Offset {offset} is not a valid position in the first Run.");
        var rect = position.GetCharacterRect(System.Windows.Documents.LogicalDirection.Forward);

        var renderScope = RequireRenderScope(page._box);
        var textView = GetInternalProperty(renderScope, "TextView")
            ?? throw new InvalidOperationException("FlowDocumentView.TextView is not available.");
        var method = textView.GetType().GetMethod(
            "GetTextPositionFromPoint",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            binder: null,
            types: [typeof(Windows.Foundation.Point), typeof(bool)],
            modifiers: null)
            ?? throw new InvalidOperationException("ITextView.GetTextPositionFromPoint not found.");
        var hitPoint = new Windows.Foundation.Point(rect.X + 1, rect.Y + rect.Height / 2);
        var hitPosition = (System.Windows.Documents.TextPointer)method.Invoke(textView, [hitPoint, true])!;
        var hitOffset = run.ContentStart.GetOffsetToPosition(hitPosition);

        return $"{{\"rectX\":{rect.X.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"rectY\":{rect.Y.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"rectWidth\":{rect.Width.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"rectHeight\":{rect.Height.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"requestedOffset\":{offset},\"hitOffset\":{hitOffset}}}";
    });

    static System.Windows.Documents.IRichTextDragDropHost RequireDragDropHost(WpfRichTextBox box) =>
        box as System.Windows.Documents.IRichTextDragDropHost
            ?? throw new InvalidOperationException("RichTextBox does not implement IRichTextDragDropHost.");

    [DevFlowAction("richtextbox.probe.drag-drop-selection-range", Description = "Call IRichTextDragDropHost.GetSelectionRange() for the current RichTextBox.")]
    public static string ProbeDragDropSelectionRange() => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created.");

        var (min, max) = RequireDragDropHost(page._box).GetSelectionRange();
        return $"{{\"min\":{min},\"max\":{max}}}";
    });

    [DevFlowAction("richtextbox.probe.drag-drop-get-text-range", Description = "Call IRichTextDragDropHost.GetTextRange(start, end) for the current RichTextBox.")]
    public static string ProbeDragDropGetTextRange(int start, int end) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created.");

        var text = RequireDragDropHost(page._box).GetTextRange(start, end);
        return $"{{\"text\":{Js(text)}}}";
    });

    [DevFlowAction("richtextbox.probe.drag-drop-insert-text-at", Description = "Call IRichTextDragDropHost.InsertTextAt(offset, text) for the current RichTextBox, simulating a drop.")]
    public static string ProbeDragDropInsertTextAt(int offset, string text) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created.");

        RequireDragDropHost(page._box).InsertTextAt(offset, text);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.drag-drop-hit-test-at-offset", Description = "Compute the character rect at an offset in the first Run and call IRichTextDragDropHost.HitTest at its point.")]
    public static string ProbeDragDropHitTestAtOffset(int offset) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created.");

        var document = page._box.Document ?? throw new InvalidOperationException("RichTextBox has no Document.");
        var paragraph = document.Blocks.FirstBlock as WpfParagraph ?? throw new InvalidOperationException("First block is not a Paragraph.");
        var run = FirstRun(paragraph.Inlines.FirstInline) ?? throw new InvalidOperationException("First Paragraph does not contain a plain Run.");
        var position = run.ContentStart.GetPositionAtOffset(offset)
            ?? throw new InvalidOperationException($"Offset {offset} is not a valid position in the first Run.");
        var rect = position.GetCharacterRect(System.Windows.Documents.LogicalDirection.Forward);
        var point = new Windows.Foundation.Point(rect.X + 1, rect.Y + rect.Height / 2);

        var hitOffset = RequireDragDropHost(page._box).HitTest(point);
        return $"{{\"hitOffset\":{hitOffset}}}";
    });

    [DevFlowAction("richtextbox.probe.select-first-list-item", Description = "Select a range inside the first ListItem's Run for list command probes.")]
    public static string ProbeSelectFirstListItem(int start, int length) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.set-list-document first.");
        if (page._box.Document?.Blocks.FirstBlock is not System.Windows.Documents.List list)
            throw new InvalidOperationException("Current document's first block is not a List.");
        if (list.ListItems.FirstListItem is not { } firstItem)
            throw new InvalidOperationException("List has no items.");

        if (firstItem.Blocks.FirstBlock is not WpfParagraph paragraph || paragraph.Inlines.FirstInline is not WpfRun run)
            throw new InvalidOperationException("First ListItem does not contain a plain Run.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        var rangeStart = run.ContentStart.GetPositionAtOffset(start) ?? run.ContentStart;
        var rangeEnd = length == 0 ? rangeStart : (run.ContentStart.GetPositionAtOffset(start + length) ?? run.ContentEnd);
        page._box.Selection.Select(rangeStart, rangeEnd);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.select-second-list-item", Description = "Select a range inside the second ListItem's Run for list command probes.")]
    public static string ProbeSelectSecondListItem(int start, int length) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.set-list-document first.");
        if (page._box.Document?.Blocks.FirstBlock is not System.Windows.Documents.List list)
            throw new InvalidOperationException("Current document's first block is not a List.");
        if (list.ListItems.Count < 2)
            throw new InvalidOperationException("List has fewer than 2 items.");

        var secondItem = list.ListItems.FirstListItem!.NextListItem!;
        if (secondItem.Blocks.FirstBlock is not WpfParagraph paragraph || paragraph.Inlines.FirstInline is not WpfRun run)
            throw new InvalidOperationException("Second ListItem does not contain a plain Run.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        var rangeStart = run.ContentStart.GetPositionAtOffset(start) ?? run.ContentStart;
        var rangeEnd = length == 0 ? rangeStart : (run.ContentStart.GetPositionAtOffset(start + length) ?? run.ContentEnd);
        page._box.Selection.Select(rangeStart, rangeEnd);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.character-received", Description = "Invoke RichTextBox.OnCharacterReceived with a real Uno CharacterReceivedRoutedEventArgs for the current RichTextBox.")]
    public static string ProbeCharacterReceived(string text) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        var ctor = typeof(Microsoft.UI.Xaml.Input.CharacterReceivedRoutedEventArgs).GetConstructor(
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic,
            binder: null,
            types: [typeof(char), typeof(global::Windows.UI.Core.CorePhysicalKeyStatus)],
            modifiers: null)
            ?? throw new InvalidOperationException("CharacterReceivedRoutedEventArgs constructor not found.");
        var method = typeof(WpfRichTextBox).GetMethod(
            "OnCharacterReceived",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            binder: null,
            types: [typeof(Microsoft.UI.Xaml.Input.CharacterReceivedRoutedEventArgs)],
            modifiers: null)
            ?? throw new InvalidOperationException("RichTextBox.OnCharacterReceived not found.");

        foreach (var c in text)
        {
            var args = ctor.Invoke([c, default(global::Windows.UI.Core.CorePhysicalKeyStatus)]);
            method.Invoke(page._box, [args]);
        }
        page._box.UpdateLayout();
        return Snapshot(page);
    });

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

    static CoreTextEditContext RequireImeContext(WpfRichTextBox box)
    {
        var field = typeof(WpfRichTextBox).GetField("_imeContext", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("RichTextBox._imeContext field not found.");
        return field.GetValue(box) as CoreTextEditContext
            ?? throw new InvalidOperationException("RichTextBox._imeContext is null (IME context not attached).");
    }

    [DevFlowAction("richtextbox.probe.ime-context-state", Description = "Report whether the current RichTextBox has an attached CoreTextEditContext.")]
    public static string ProbeImeContextState() => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created.");
        var field = typeof(WpfRichTextBox).GetField("_imeContext", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var context = field?.GetValue(page._box);
        return $"{{\"hasImeContext\":{Jb(context is not null)}}}";
    });

    [DevFlowAction("richtextbox.probe.simulate-ime-text-updating", Description = "Directly raise CoreTextEditContext.TextUpdating (simulating the platform IME committing composed text) for the current RichTextBox's whole-document range.")]
    public static string ProbeSimulateImeTextUpdating(string newText, int rangeStart, int rangeEnd) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain first.");

        var context = RequireImeContext(page._box);
        var args = new CoreTextTextUpdatingEventArgs(newText)
        {
            Range = new CoreTextRange { StartCaretPosition = rangeStart, EndCaretPosition = rangeEnd },
            NewSelection = new CoreTextRange { StartCaretPosition = rangeStart + newText.Length, EndCaretPosition = rangeStart + newText.Length },
        };
        context.RaiseTextUpdating(args);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.simulate-ime-command", Description = "Directly raise CoreTextEditContext.CommandReceived (simulating an AppKit doCommandBySelector: callback) for the current RichTextBox.")]
    public static string ProbeSimulateImeCommand(string command) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain first.");

        var context = RequireImeContext(page._box);
        var eventArgs = new CoreTextCommandReceivedEventArgs(command);
        var raiseMethod = typeof(CoreTextEditContext).GetMethod("RaiseCommandReceived", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
            ?? throw new InvalidOperationException("CoreTextEditContext.RaiseCommandReceived not found.");
        raiseMethod.Invoke(context, [eventArgs]);
        page._box.UpdateLayout();
        return $"{{\"handled\":{Jb(eventArgs.Handled)},\"snapshot\":{Snapshot(page)}}}";
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

    [DevFlowAction("richtextbox.probe.copy-run-range", Description = "Select a range inside the first Run and invoke RichTextBox.Copy.")]
    public static string ProbeCopyRunRange(int start, int length) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        SelectFirstRunTextRange(page._box, start, length);
        page._box.Copy();
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.cut-run-range", Description = "Select a range inside the first Run and invoke RichTextBox.Cut.")]
    public static string ProbeCutRunRange(int start, int length) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        SelectFirstRunTextRange(page._box, start, length);
        page._box.Cut();
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.paste-text", Description = "Set shim clipboard text and invoke RichTextBox.Paste at the current selection.")]
    public static string ProbePasteText(string text) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        System.Windows.Clipboard.SetText(text);
        page._box.Paste();
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.paste-text-at-run-offset", Description = "Place the caret inside the first Run, set shim clipboard text, and invoke RichTextBox.Paste.")]
    public static string ProbePasteTextAtRunOffset(string text, int offset) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        SelectFirstRunTextRange(page._box, offset, 0);
        System.Windows.Clipboard.SetText(text);
        page._box.Paste();
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.set-caret-run-offset", Description = "Place the caret at an offset inside the first Run.")]
    public static string ProbeSetCaretRunOffset(int offset) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        SelectFirstRunTextRange(page._box, offset, 0);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.select-run-range", Description = "Select a non-empty range inside the first Run.")]
    public static string ProbeSelectRunRange(int start, int length) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        SelectFirstRunTextRange(page._box, start, length);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.set-caret-on-mouse-event-at-offset", Description = "Call TextEditorMouse.SetCaretPositionOnMouseEvent directly at the character rect for an offset in the first Run, with an explicit clickCount (1=place caret, 2=select word, 3=select paragraph).")]
    public static string ProbeSetCaretOnMouseEventAtOffset(int offset, int clickCount) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        var document = page._box.Document ?? throw new InvalidOperationException("RichTextBox has no Document.");
        var paragraph = document.Blocks.FirstBlock as WpfParagraph ?? throw new InvalidOperationException("First block is not a Paragraph.");
        var run = FirstRun(paragraph.Inlines.FirstInline) ?? throw new InvalidOperationException("First Paragraph does not contain a plain Run.");
        var position = run.ContentStart.GetPositionAtOffset(offset)
            ?? throw new InvalidOperationException($"Offset {offset} is not a valid position in the first Run.");
        var rect = position.GetCharacterRect(System.Windows.Documents.LogicalDirection.Forward);
        var point = new Windows.Foundation.Point(rect.X + 1, rect.Y + rect.Height / 2);

        var textEditor = RequireTextEditor(page._box);
        var textEditorTypingType = typeof(WpfRichTextBox).Assembly.GetType("System.Windows.Documents.TextEditorMouse")
            ?? throw new InvalidOperationException("TextEditorMouse type not found.");
        var method = textEditorTypingType.GetMethod(
            "SetCaretPositionOnMouseEvent",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("TextEditorMouse.SetCaretPositionOnMouseEvent not found.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        method.Invoke(null, [textEditor, point, System.Windows.Input.MouseButton.Left, clickCount]);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.compute-click-count", Description = "Call RichTextBox's private ComputeClickCount(timestamp, point) directly to verify double/triple-click detection heuristics.")]
    public static string ProbeComputeClickCount(long timestampMicroseconds, double x, double y) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created.");

        var method = typeof(WpfRichTextBox).GetMethod(
            "ComputeClickCount",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("RichTextBox.ComputeClickCount not found.");
        var point = new Windows.Foundation.Point(x, y);
        var clickCount = (int)method.Invoke(page._box, [(ulong)timestampMicroseconds, point])!;
        return $"{{\"clickCount\":{clickCount}}}";
    });

    [DevFlowAction("richtextbox.probe.save-load-format-roundtrip", Description = "Save the current document to a stream in the given DataFormats value, load it into a fresh FlowDocument, and swap it in.")]
    public static string ProbeSaveLoadFormatRoundtrip(string format) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");
        if (page._box.Document is not { } document)
            throw new InvalidOperationException("RichTextBox has no Document.");

        using var stream = new System.IO.MemoryStream();
        var sourceRange = new WpfTextRange(document.ContentStart, document.ContentEnd);
        sourceRange.Save(stream, format);
        stream.Position = 0;

        var reloaded = new System.Windows.Documents.FlowDocument();
        var targetRange = new WpfTextRange(reloaded.ContentStart, reloaded.ContentEnd);
        targetRange.Load(stream, format);

        page._box.Document = reloaded;
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.can-save-load-format", Description = "Report CanSave/CanLoad for a given DataFormats value against the current document range.")]
    public static string ProbeCanSaveLoadFormat(string format) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");
        if (page._box.Document is not { } document)
            throw new InvalidOperationException("RichTextBox has no Document.");

        var range = new WpfTextRange(document.ContentStart, document.ContentEnd);
        var canSave = range.CanSave(format);
        var canLoad = range.CanLoad(format);
        return $"{{\"canSave\":{Jb(canSave)},\"canLoad\":{Jb(canLoad)}}}";
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

    [DevFlowAction("richtextbox.probe.apply-inline-flow-direction-ltr-selection-command", Description = "Select all text and invoke TextEditorCharacters' ApplyInlineFlowDirectionLTR command handler for the current RichTextBox.")]
    public static string ProbeApplyInlineFlowDirectionLtrSelectionCommand() =>
        ProbeApplyInlineFlowDirectionSelectionCommand("OnApplyInlineFlowDirectionLTR");

    [DevFlowAction("richtextbox.probe.apply-inline-flow-direction-rtl-selection-command", Description = "Select all text and invoke TextEditorCharacters' ApplyInlineFlowDirectionRTL command handler for the current RichTextBox.")]
    public static string ProbeApplyInlineFlowDirectionRtlSelectionCommand() =>
        ProbeApplyInlineFlowDirectionSelectionCommand("OnApplyInlineFlowDirectionRTL");

    static string ProbeApplyInlineFlowDirectionSelectionCommand(string methodName) => RunOnUi(page =>
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
        InvokeTextEditorCharacters(methodName, page._box, args);
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

    [DevFlowAction("richtextbox.probe.toggle-bullets-selection-command", Description = "Select all text and invoke TextEditorLists' ToggleBullets command handler for the current RichTextBox.")]
    public static string ProbeToggleBulletsSelectionCommand() => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        page._box.SelectAll();
        InvokeTextEditorListsOnListCommand(page._box, WpfDocumentEditingCommands.ToggleBullets);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.toggle-numbering-selection-command", Description = "Select all text and invoke TextEditorLists' ToggleNumbering command handler for the current RichTextBox.")]
    public static string ProbeToggleNumberingSelectionCommand() => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        page._box.SelectAll();
        InvokeTextEditorListsOnListCommand(page._box, WpfDocumentEditingCommands.ToggleNumbering);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.increase-indentation-selection-command", Description = "Select all text and invoke TextEditorLists' IncreaseIndentation command handler for the current RichTextBox.")]
    public static string ProbeIncreaseIndentationSelectionCommand() => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        page._box.SelectAll();
        InvokeTextEditorListsOnListCommand(page._box, WpfDocumentEditingCommands.IncreaseIndentation);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.decrease-indentation-selection-command", Description = "Select all text and invoke TextEditorLists' DecreaseIndentation command handler for the current RichTextBox.")]
    public static string ProbeDecreaseIndentationSelectionCommand() => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        page._box.SelectAll();
        InvokeTextEditorListsOnListCommand(page._box, WpfDocumentEditingCommands.DecreaseIndentation);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.increase-indentation-command", Description = "Invoke TextEditorLists' IncreaseIndentation command handler for the current selection, without changing it first.")]
    public static string ProbeIncreaseIndentationCommand() => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.set-list-document first.");

        InvokeTextEditorListsOnListCommand(page._box, WpfDocumentEditingCommands.IncreaseIndentation);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.decrease-indentation-command", Description = "Invoke TextEditorLists' DecreaseIndentation command handler for the current selection, without changing it first.")]
    public static string ProbeDecreaseIndentationCommand() => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.set-list-document first.");

        InvokeTextEditorListsOnListCommand(page._box, WpfDocumentEditingCommands.DecreaseIndentation);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.remove-list-markers-command", Description = "Invoke TextEditorLists' RemoveListMarkers command handler for the current selection, without changing it first.")]
    public static string ProbeRemoveListMarkersCommand() => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.set-list-document first.");

        var removeListMarkersProperty = typeof(WpfDocumentEditingCommands).GetProperty(
            "RemoveListMarkers",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("EditingCommands.RemoveListMarkers not found.");
        var removeListMarkers = (System.Windows.Input.RoutedUICommand)removeListMarkersProperty.GetValue(null)!;
        InvokeTextEditorListsOnListCommand(page._box, removeListMarkers);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.toggle-bullets-command", Description = "Invoke TextEditorLists' ToggleBullets command handler for the current selection, without changing it first.")]
    public static string ProbeToggleBulletsCommand() => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.set-list-document first.");

        InvokeTextEditorListsOnListCommand(page._box, WpfDocumentEditingCommands.ToggleBullets);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.toggle-numbering-command", Description = "Invoke TextEditorLists' ToggleNumbering command handler for the current selection, without changing it first.")]
    public static string ProbeToggleNumberingCommand() => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.set-list-document first.");

        InvokeTextEditorListsOnListCommand(page._box, WpfDocumentEditingCommands.ToggleNumbering);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

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

    [DevFlowAction("richtextbox.probe.key-down-modifiers", Description = "Invoke RichTextBox.OnKeyDown with a Uno KeyRoutedEventArgs and modifiers for the current RichTextBox.")]
    public static string ProbeKeyDownModifiers(string key, string modifiers) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        InvokeRichTextBoxOnKeyDown(page._box, ParseVirtualKey(key), ParseVirtualKeyModifiers(modifiers));
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

    [DevFlowAction("richtextbox.probe.key-down-up-select-all-modifiers", Description = "Select all text and invoke RichTextBox.OnKeyDown followed by OnKeyUp with modifiers.")]
    public static string ProbeKeyDownUpSelectAllModifiers(string key, string modifiers) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        page._box.SelectAll();
        var parsedKey = ParseVirtualKey(key);
        var parsedModifiers = ParseVirtualKeyModifiers(modifiers);
        InvokeRichTextBoxOnKeyDown(page._box, parsedKey, parsedModifiers);
        InvokeRichTextBoxOnKeyUp(page._box, parsedKey, parsedModifiers);
        page._box.UpdateLayout();
        return Snapshot(page);
    });
}
#else
public sealed partial class MainPage : Microsoft.UI.Xaml.Controls.Page
{
}
#endif
