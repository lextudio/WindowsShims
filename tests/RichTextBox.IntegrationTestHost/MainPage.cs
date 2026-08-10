#if DEBUG
using System.Threading;
using Microsoft.Maui.DevFlow.Agent.Core;
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

    static bool HasStrikethrough(object? value)
    {
        if (value is not WpfTextDecorationCollection decorations)
            return false;

        foreach (var decoration in decorations)
        {
            foreach (var strikethrough in WpfTextDecorations.Strikethrough)
            {
                if (Equals(decoration, strikethrough))
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
                var strikethrough = HasStrikethrough(run.GetValue(WpfInline.TextDecorationsProperty)) ? "S" : "-";
                var flowDirection = run.GetValue(WpfInline.FlowDirectionProperty)?.ToString();
                parts.Add($"Run:{InlineText(run.Text)}:w={FormatFontWeight(run.GetValue(WpfTextElement.FontWeightProperty))}:s={style}:z={size}:d={underline}:st={strikethrough}:fd={flowDirection}");
            }
            else if (inline is WpfSpan span)
            {
                var style = span.GetValue(WpfTextElement.FontStyleProperty)?.ToString();
                var size = span.GetValue(WpfTextElement.FontSizeProperty)?.ToString();
                var underline = HasUnderline(span.GetValue(WpfInline.TextDecorationsProperty)) ? "U" : "-";
                var strikethrough = HasStrikethrough(span.GetValue(WpfInline.TextDecorationsProperty)) ? "S" : "-";
                var flowDirection = span.GetValue(WpfInline.FlowDirectionProperty)?.ToString();
                parts.Add($"{span.GetType().Name}:w={FormatFontWeight(span.GetValue(WpfTextElement.FontWeightProperty))}:s={style}:z={size}:d={underline}:st={strikethrough}:fd={flowDirection}");
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
        var firstHyperlinkNavigateUri = FindFirstHyperlinkNavigateUri(document);
        var firstList = document?.Blocks.FirstBlock as System.Windows.Documents.List;
        var firstListMarkerStyle = firstList?.MarkerStyle.ToString();
        var firstListStartIndex = firstList?.StartIndex;
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
        var firstTable = document?.Blocks.FirstBlock as System.Windows.Documents.Table;
        var firstTableColumnWidths = firstTable is null
            ? null
            : string.Join(",", firstTable.Columns.Select(c => c.Width.Value.ToString(global::System.Globalization.CultureInfo.InvariantCulture)));
        var firstTableCell = FirstTableCell(firstTable);
        var firstTableCellBackground = firstTableCell is null
            ? null
            : FormatBrush(firstTableCell.GetValue(System.Windows.Documents.TableCell.BackgroundProperty));
        var firstTableCellBorderThickness = firstTableCell?.BorderThickness.ToString();
        var firstTableCellBorderBrush = firstTableCell is null
            ? null
            : FormatBrush(firstTableCell.GetValue(System.Windows.Documents.TableCell.BorderBrushProperty));
        var firstTableCellPadding = firstTableCell?.Padding.ToString();
        var firstTableCellRowSpan = firstTableCell?.RowSpan;
        var firstTableCellColumnSpan = firstTableCell?.ColumnSpan;
        var firstTableCellHasNestedTable = firstTableCell?.Blocks.OfType<System.Windows.Documents.Table>().Any() == true;
        var firstParagraphTextAlignment = firstParagraph?.TextAlignment.ToString();
        var firstParagraphLineHeight = firstParagraph?.LineHeight.ToString();
        var firstParagraphLineStackingStrategy = firstParagraph?.LineStackingStrategy.ToString();
        var firstParagraphFlowDirection = firstParagraph?.FlowDirection.ToString();
        var firstParagraphFontSize = firstParagraph is null
            ? null
            : firstParagraph.GetValue(WpfTextElement.FontSizeProperty)?.ToString();
        var firstParagraphMargin = firstParagraph?.Margin.ToString();
        var firstParagraphTextIndent = firstParagraph?.TextIndent.ToString();
        var firstParagraphBorderThickness = firstParagraph?.BorderThickness.ToString();
        var firstParagraphBorderBrush = firstParagraph is null
            ? null
            : FormatBrush(firstParagraph.GetValue(System.Windows.Documents.Block.BorderBrushProperty));
        var inlineTree = FormatInlineTree(firstInline);
        var firstInlineImageDims = FindFirstImageDims(firstInline);
        var firstBlockImageDims = FindFirstBlockImageDims(document?.Blocks.FirstBlock);
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
        var firstInlineVariants = firstInline is null
            ? null
            : firstInline.GetValue(System.Windows.Documents.Typography.VariantsProperty)?.ToString();
        var firstInlineLanguage = firstInline is null
            ? null
            : firstInline.GetValue(Microsoft.UI.Xaml.FrameworkElement.LanguageProperty)?.ToString();
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

        return $"{{\"hasRichTextBox\":{Jb(box is not null)},\"hasDocument\":{Jb(document is not null)},\"blockCount\":{(document?.Blocks.Count ?? 0)},\"text\":{Js(text)},\"canUndo\":{Jb(canUndo)},\"canRedo\":{Jb(canRedo)},\"selectionText\":{Js(selectionText)},\"selectionFontWeight\":{Js(selectionFontWeight)},\"selectionStartRunOffset\":{(selectionStartRunOffset?.ToString() ?? "null")},\"selectionEndRunOffset\":{(selectionEndRunOffset?.ToString() ?? "null")},\"clipboardText\":{Js(clipboardText)},\"firstBlockType\":{Js(firstBlockType)},\"firstHyperlinkNavigateUri\":{Js(firstHyperlinkNavigateUri)},\"firstListMarkerStyle\":{Js(firstListMarkerStyle)},\"firstListStartIndex\":{(firstListStartIndex?.ToString() ?? "null")},\"firstListItemCount\":{(firstListItemCount?.ToString() ?? "null")},\"firstListItemText\":{Js(firstListItemText)},\"firstListItemBlockTypes\":{Js(firstListItemBlockTypes)},\"nestedListMarkerStyle\":{Js(nestedListMarkerStyle)},\"nestedListItemCount\":{(nestedListItemCount?.ToString() ?? "null")},\"firstTableCellBackground\":{Js(firstTableCellBackground)},\"firstTableCellBorderThickness\":{Js(firstTableCellBorderThickness)},\"firstTableCellBorderBrush\":{Js(firstTableCellBorderBrush)},\"firstTableCellPadding\":{Js(firstTableCellPadding)},\"firstTableCellRowSpan\":{(firstTableCellRowSpan?.ToString() ?? "null")},\"firstTableCellColumnSpan\":{(firstTableCellColumnSpan?.ToString() ?? "null")},\"firstTableCellHasNestedTable\":{Jb(firstTableCellHasNestedTable)},\"firstTableColumnWidths\":{Js(firstTableColumnWidths)},\"firstParagraphTextAlignment\":{Js(firstParagraphTextAlignment)},\"firstParagraphLineHeight\":{Js(firstParagraphLineHeight)},\"firstParagraphLineStackingStrategy\":{Js(firstParagraphLineStackingStrategy)},\"firstParagraphFlowDirection\":{Js(firstParagraphFlowDirection)},\"firstParagraphFontSize\":{Js(firstParagraphFontSize)},\"firstParagraphMargin\":{Js(firstParagraphMargin)},\"firstParagraphTextIndent\":{Js(firstParagraphTextIndent)},\"firstParagraphBorderThickness\":{Js(firstParagraphBorderThickness)},\"firstParagraphBorderBrush\":{Js(firstParagraphBorderBrush)},\"inlineTree\":{Js(inlineTree)},\"firstInlineImageDims\":{Js(firstInlineImageDims)},\"firstBlockImageDims\":{Js(firstBlockImageDims)},\"firstInlineType\":{Js(firstInline?.GetType().FullName)},\"firstInlineFontWeight\":{Js(firstInlineFontWeight)},\"firstInlineFontStyle\":{Js(firstInlineFontStyle)},\"firstInlineFontSize\":{Js(firstInlineFontSize)},\"firstInlineFontFamily\":{Js(firstInlineFontFamily)},\"firstInlineForeground\":{Js(firstInlineForeground)},\"firstInlineBackground\":{Js(firstInlineBackground)},\"firstInlineFlowDirection\":{Js(firstInlineFlowDirection)},\"firstInlineVariants\":{Js(firstInlineVariants)},\"firstInlineLanguage\":{Js(firstInlineLanguage)},\"firstInlineHasUnderline\":{Jb(firstInlineHasUnderline)},\"firstRunFontWeight\":{Js(firstRunFontWeight)},\"firstRunFontStyle\":{Js(firstRunFontStyle)},\"firstRunFontSize\":{Js(firstRunFontSize)},\"firstRunFontFamily\":{Js(firstRunFontFamily)},\"firstRunForeground\":{Js(firstRunForeground)},\"firstRunBackground\":{Js(firstRunBackground)},\"firstRunFlowDirection\":{Js(firstRunFlowDirection)},\"firstRunHasUnderline\":{Jb(firstRunHasUnderline)},\"contentHostAvailable\":{Jb(contentHostAvailable)},\"renderScopeType\":{Js(renderScope?.GetType().FullName)},\"textViewType\":{Js(textView?.GetType().FullName)}}}";
    }

    static string? FindFirstHyperlinkNavigateUri(System.Windows.Documents.FlowDocument? document)
    {
        if (document?.Blocks.FirstBlock is not WpfParagraph paragraph)
            return null;
        return FindHyperlinkUri(paragraph.Inlines.FirstInline);
    }

    static System.Windows.Documents.TableCell? FirstTableCell(System.Windows.Documents.Table? table)
    {
        if (table is null)
            return null;
        foreach (var rg in table.RowGroups)
        {
            foreach (var row in rg.Rows)
            {
                if (row.Cells.Count > 0)
                    return row.Cells[0];
            }
        }
        return null;
    }

    static string? FindFirstBlockImageDims(System.Windows.Documents.Block? block)
    {
        var current = block;
        while (current is not null)
        {
            if (current is System.Windows.Documents.BlockUIContainer { Child: System.Windows.Controls.Image { Source: System.Windows.Media.Imaging.BitmapSource bs } })
                return $"{bs.PixelWidth}x{bs.PixelHeight}";
            // RTF has no block-level image concept; block images reload as inline.
            if (current is System.Windows.Documents.Paragraph { Inlines.FirstInline: { } firstInline } && FindFirstImageDims(firstInline) is { } inlineDims)
                return inlineDims;
            current = current.NextBlock;
        }
        return null;
    }

    static string? FindFirstImageDims(WpfInline? inline)
    {
        while (inline is not null)
        {
            if (inline is System.Windows.Documents.InlineUIContainer { Child: System.Windows.Controls.Image { Source: System.Windows.Media.Imaging.BitmapSource bs } })
                return $"{bs.PixelWidth}x{bs.PixelHeight}";
            if (inline is WpfSpan span && FindFirstImageDims(span.Inlines.FirstInline) is { } nested)
                return nested;
            inline = inline.NextInline;
        }
        return null;
    }

    static string? FindHyperlinkUri(WpfInline? inline)
    {
        while (inline is not null)
        {
            if (inline is System.Windows.Documents.Hyperlink hyperlink)
                return hyperlink.NavigateUri?.ToString();
            if (inline is WpfSpan span && FindHyperlinkUri(span.Inlines.FirstInline) is { } nested)
                return nested;
            inline = inline.NextInline;
        }
        return null;
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
            if (string.Equals(part, "Cmd", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(part, "Command", StringComparison.OrdinalIgnoreCase))
            {
                // Cmd/Command on macOS → VirtualKeyModifiers.Windows in Uno/WinUI
                result |= global::Windows.System.VirtualKeyModifiers.Windows;
                continue;
            }
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
        // On macOS the physical Cmd key reports as Windows/Command; map it to
        // Control so WPF command key-gestures (Ctrl+A, Ctrl+C, etc.) resolve.
        if ((modifiers & global::Windows.System.VirtualKeyModifiers.Windows) != 0)
            result |= System.Windows.Input.ModifierKeys.Control;
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

    [DevFlowAction("richtextbox.probe.table-collection-counts", Description = "Report row/cell collection counts for the current table document.")]
    public static string ProbeTableCollectionCounts() => RunOnUi(page =>
    {
        if (page._box?.Document is not { } document)
            return "{\"error\":\"no document\"}";
        var block = document.Blocks.FirstBlock;
        var table = block as System.Windows.Documents.Table;
        if (table is null)
            return "{\"error\":\"no table\"}";
        var rowGroupCount = table.RowGroups.Count;
        var rowCount = rowGroupCount > 0 ? table.RowGroups[0].Rows.Count : 0;
        var cellCount = rowCount > 0 ? table.RowGroups[0].Rows[0].Cells.Count : 0;
        return $"{{\"rowGroupCount\":{rowGroupCount},\"rowCount\":{rowCount},\"cellCount\":{cellCount}}}";
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

    [DevFlowAction("richtextbox.probe.set-nested-inline-document", Description = "Create a RichTextBox with a FlowDocument containing a paragraph with nested inlines: plain, Bold, plain, Italic, plain.")]
    public static string ProbeSetNestedInlineDocument() => RunOnUi(page =>
    {
        var box = new WpfRichTextBox
        {
            Width = 640,
            Height = 240,
            AcceptsReturn = true,
            Document = RichTextBoxScenarios.BuildNestedInlineDocument(),
        };
        page._root.Children.Clear();
        page._box = box;
        page._root.Children.Add(box);
        box.ApplyTemplate();
        box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.set-bold-inside-italic-document", Description = "Create a RichTextBox with a FlowDocument containing Bold nested inside Italic.")]
    public static string ProbeSetBoldInsideItalicDocument() => RunOnUi(page =>
    {
        var box = new WpfRichTextBox
        {
            Width = 640,
            Height = 240,
            AcceptsReturn = true,
            Document = RichTextBoxScenarios.BuildBoldInsideItalicDocument(),
        };
        page._root.Children.Clear();
        page._box = box;
        page._root.Children.Add(box);
        box.ApplyTemplate();
        box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.set-inlineui-document", Description = "Create a RichTextBox with an InlineUIContainer (Button) in a paragraph.")]
    public static string ProbeSetInlineUiDocument() => RunOnUi(page =>
    {
        var box = new WpfRichTextBox
        {
            Width = 640,
            Height = 240,
            AcceptsReturn = true,
            Document = RichTextBoxScenarios.BuildInlineUiContainerDocument(),
        };
        page._root.Children.Clear();
        page._box = box;
        page._root.Children.Add(box);
        box.ApplyTemplate();
        box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.set-blockui-document", Description = "Create a RichTextBox with a BlockUIContainer (Button) between paragraphs.")]
    public static string ProbeSetBlockUiDocument() => RunOnUi(page =>
    {
        var box = new WpfRichTextBox
        {
            Width = 640,
            Height = 240,
            AcceptsReturn = true,
            Document = RichTextBoxScenarios.BuildBlockUiContainerDocument(),
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

    [DevFlowAction("richtextbox.probe.drag-drop-end-to-end", Description = "Simulate a full drag-drop: extract selection text and insert it at a target offset in the first Run, mirroring the OnDragStarting→OnDragOver→OnDrop flow.")]
    public static string ProbeDragDropEndToEnd(int targetOffset) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created.");

        var host = RequireDragDropHost(page._box);
        var document = page._box.Document ?? throw new InvalidOperationException("RichTextBox has no Document.");

        var (selMin, selMax) = host.GetSelectionRange();
        if (selMin < 0 || selMin == selMax)
            return Snapshot(page);

        var draggedText = host.GetTextRange(selMin, selMax);

        var paragraph = document.Blocks.FirstBlock as WpfParagraph ?? throw new InvalidOperationException("First block is not a Paragraph.");
        var run = FirstRun(paragraph.Inlines.FirstInline) ?? throw new InvalidOperationException("First Paragraph does not contain a plain Run.");
        var targetPos = run.ContentStart.GetPositionAtOffset(targetOffset)
            ?? throw new InvalidOperationException($"Target offset {targetOffset} is not valid.");
        var rect = targetPos.GetCharacterRect(System.Windows.Documents.LogicalDirection.Forward);
        var point = new Windows.Foundation.Point(rect.X + 1, rect.Y + rect.Height / 2);
        var hitOffset = host.HitTest(point);
        host.SetDropCaretOffset(hitOffset);

        host.InsertTextAt(hitOffset, draggedText);

        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.set-is-read-only", Description = "Set RichTextBox.IsReadOnly to true or false.")]
    public static string ProbeSetIsReadOnly(bool value) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.IsReadOnly = value;
        // Force the view to update its read-only state
        var rs = GetInternalProperty(page._box, "RenderScope");
        if (rs is not null)
        {
            var prop = rs.GetType().GetProperty("ReadOnly", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (prop is not null)
                prop.SetValue(rs, value);
            rs.GetType().GetMethod("InvalidateArrange", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)?.Invoke(rs, null);
        }
        return Snapshot(page);
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

    [DevFlowAction("richtextbox.probe.set-ime-composition-range", Description = "Set the IME composition range on the FlowDocumentView and trigger visual update.")]
    public static string ProbeSetImeCompositionRange(int start, int length) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created.");

        var view = RequireRenderScope(page._box);

        var setRangeMethod = view.GetType().GetMethod("SetImeCompositionRange", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)
            ?? throw new InvalidOperationException("FlowDocumentView.SetImeCompositionRange not found.");
        setRangeMethod.Invoke(view, [start, length]);
        view.GetType().GetMethod("InvalidateArrange", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)?.Invoke(view, null);
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.get-ime-underline-count", Description = "Report how many visible IME composition underline Line elements exist in the FlowDocumentView.")]
    public static string ProbeGetImeUnderlineCount() => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created.");

        var view = RequireRenderScope(page._box);
        var viewType = view.GetType().FullName;

        var countProp = view.GetType().GetProperty("ImeUnderlineLineCount", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)
            ?? throw new InvalidOperationException($"FlowDocumentView.ImeUnderlineLineCount not found (view type: {viewType}).");
        var count = (int)countProp.GetValue(view)!;
        return $"{{\"count\":{count}}}";
    });

    static string SpellCheckSnapshot(MainPage page)
    {
        var box = page._box;
        if (box is null)
            return "{\"error\":\"no box\"}";
        box.UpdateLayout();

        var spellCheckField = typeof(WpfRichTextBox).GetField("_spellCheckEnabled", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("RichTextBox._spellCheckEnabled not found.");
        bool enabled = (bool)spellCheckField.GetValue(box)!;

        var view = RequireRenderScope(box);
        var squiggleCountProp = view.GetType().GetProperty("SpellCheckSquiggleCount", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)
            ?? throw new InvalidOperationException("FlowDocumentView.SpellCheckSquiggleCount not found.");
        int squiggleCount = (int)squiggleCountProp.GetValue(view)!;

        var linesField = view.GetType().GetField("_spellCheckLines", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("FlowDocumentView._spellCheckLines not found.");
        var lines = (System.Collections.IEnumerable)linesField.GetValue(view)!;
        var ranges = new List<string>();
        foreach (var line in lines)
        {
            var visibility = line.GetType().GetProperty("Visibility")?.GetValue(line);
            if (visibility is not Microsoft.UI.Xaml.Visibility.Visible)
                continue;
            var points = (System.Collections.IEnumerable)line.GetType().GetProperty("Points")!.GetValue(line)!;
            string? x1 = null, x2 = null;
            int count = 0;
            foreach (var point in points)
            {
                var x = (double)point.GetType().GetProperty("X")!.GetValue(point)!;
                if (count == 0) x1 = x.ToString(System.Globalization.CultureInfo.InvariantCulture);
                x2 = x.ToString(System.Globalization.CultureInfo.InvariantCulture);
                count++;
            }
            ranges.Add($"{{\"x1\":{x1 ?? "0"},\"x2\":{x2 ?? "0"}}}");
        }

        return $"{{\"spellCheckEnabled\":{Jb(enabled)},\"squiggleCount\":{squiggleCount},\"squiggleRanges\":[{string.Join(",", ranges)}]}}";
    }

    [DevFlowAction("richtextbox.probe.set-spellcheck-document", Description = "Create a RichTextBox with plain text and enable WPF SpellCheck.IsEnabled on it.")]
    public static string ProbeSetSpellCheckDocument(string text) => RunOnUi(page =>
    {
        var box = RichTextBoxScenarios.BuildPlainTextBox(text);
        box.SpellCheck.IsEnabled = true;
        page._root.Children.Clear();
        page._box = box;
        page._root.Children.Add(box);
        box.ApplyTemplate();
        box.UpdateLayout();
        return SpellCheckSnapshot(page);
    });

    [DevFlowAction("richtextbox.probe.set-spellcheck", Description = "Set WPF SpellCheck.IsEnabled on the current RichTextBox.")]
    public static string ProbeSetSpellCheck(bool enabled) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created.");
        page._box.SpellCheck.IsEnabled = enabled;
        page._box.UpdateLayout();
        return SpellCheckSnapshot(page);
    });

    [DevFlowAction("richtextbox.probe.get-florence-line-count", Description = "Report how many lines the Florence layout engine produced for the current document.")]
    public static string ProbeGetFlorenceLineCount() => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created.");

        var view = RequireRenderScope(page._box);
        var document = page._box.Document;
        int tableCount = 0;
        int cellCount = 0;
        if (document is not null)
        {
            foreach (var block in document.Blocks)
            {
                if (block is System.Windows.Documents.Table table)
                {
                    tableCount++;
                    var rowGroups = table.RowGroups;
                    cellCount = rowGroups.Count; // store rowGroup count temporarily
                }
            }
        }
        var pageProp = view.GetType().GetProperty("Page", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)
            ?? throw new InvalidOperationException("FlowDocumentView.Page not found.");
        var florencePage = pageProp.GetValue(view);
        if (florencePage is null)
            return $"{{\"count\":0,\"lines\":[],\"tableCount\":{tableCount},\"cellCount\":{cellCount}}}";

        var linesProp = florencePage.GetType().GetProperty("Lines", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)
            ?? throw new InvalidOperationException("FlorencePage.Lines not found.");
        var lines = (System.Collections.IList)linesProp.GetValue(florencePage)!;
        var texts = new List<string>();
        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var fullTextProp = line.GetType().GetProperty("FullText", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)
                ?? throw new InvalidOperationException("FlorenceLine.FullText not found.");
            texts.Add((string)fullTextProp.GetValue(line)!);
        }
        var linesJson = string.Join(",", texts.Select(Js));
        return $"{{\"count\":{lines.Count},\"lines\":[{linesJson}],\"tableCount\":{tableCount},\"cellCount\":{cellCount}}}";
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

    [DevFlowAction("richtextbox.probe.select-text-range", Description = "Select a range by plain-text character offsets (cross-element boundary). Uses GetPositionAtPlainTextOffset.")]
    public static string ProbeSelectTextRange(int startOffset, int endOffset) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        var document = page._box.Document ?? throw new InvalidOperationException("RichTextBox has no Document.");
        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);

        var getPosMethod = typeof(WpfRichTextBox).GetMethod("GetPositionAtPlainTextOffset", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("RichTextBox.GetPositionAtPlainTextOffset not found.");
        var start = (System.Windows.Documents.TextPointer)getPosMethod.Invoke(null, [document, startOffset]);
        var end = (System.Windows.Documents.TextPointer)getPosMethod.Invoke(null, [document, endOffset]);

        if (page._box.Selection is { } sel)
        {
            sel.Select(start, end);
        }
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

    [DevFlowAction("richtextbox.probe.toggle-bold-selection-command", Description = "Select all text and invoke ToggleBold command on the RichTextBox.")]
    public static string ProbeToggleBoldSelectionCommand() => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        page._box.SelectAll();
        WpfDocumentEditingCommands.ToggleBold.Execute(null, page._box);
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

    static string ProbeToggleRunRangeCommand(string methodName, System.Windows.Input.RoutedCommand command, int start, int length) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        SelectFirstRunTextRange(page._box, start, length);
        command.Execute(null, page._box);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.toggle-italic-selection-command", Description = "Select all text and invoke ToggleItalic command on the RichTextBox.")]
    public static string ProbeToggleItalicSelectionCommand() => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        page._box.SelectAll();
        WpfDocumentEditingCommands.ToggleItalic.Execute(null, page._box);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.toggle-underline-selection-command", Description = "Select all text and invoke ToggleUnderline command on the RichTextBox.")]
    public static string ProbeToggleUnderlineSelectionCommand() => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        page._box.SelectAll();
        WpfDocumentEditingCommands.ToggleUnderline.Execute(null, page._box);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.apply-inline-flow-direction-ltr-selection-command", Description = "Select all text and invoke ApplyInlineFlowDirectionLTR command on the RichTextBox.")]
    public static string ProbeApplyInlineFlowDirectionLtrSelectionCommand() =>
        ProbeApplyInlineFlowDirectionSelectionCommand(GetEditingCommand("ApplyInlineFlowDirectionLTR"));

    [DevFlowAction("richtextbox.probe.apply-inline-flow-direction-rtl-selection-command", Description = "Select all text and invoke ApplyInlineFlowDirectionRTL command on the RichTextBox.")]
    public static string ProbeApplyInlineFlowDirectionRtlSelectionCommand() =>
        ProbeApplyInlineFlowDirectionSelectionCommand(GetEditingCommand("ApplyInlineFlowDirectionRTL"));

    static System.Windows.Input.RoutedUICommand GetEditingCommand(string propertyName)
    {
        var prop = typeof(WpfDocumentEditingCommands).GetProperty(
            propertyName,
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)
            ?? throw new InvalidOperationException($"EditingCommands.{propertyName} not found.");
        return (System.Windows.Input.RoutedUICommand)prop.GetValue(null)!;
    }

    static string ProbeApplyInlineFlowDirectionSelectionCommand(System.Windows.Input.RoutedCommand command) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        page._box.SelectAll();
        command.Execute(null, page._box);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    static string ProbeApplyParagraphFlowDirectionSelectionCommand(System.Windows.Input.RoutedCommand command) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        page._box.SelectAll();
        command.Execute(null, page._box);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.apply-font-size-selection-command", Description = "Select all text and invoke ApplyFontSize command on the RichTextBox.")]
    public static string ProbeApplyFontSizeSelectionCommand(double fontSize) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        page._box.SelectAll();
        GetEditingCommand("ApplyFontSize").Execute(fontSize, page._box);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.increase-font-size-selection-command", Description = "Select all text and invoke IncreaseFontSize command on the RichTextBox.")]
    public static string ProbeIncreaseFontSizeSelectionCommand() => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        page._box.SelectAll();
        WpfDocumentEditingCommands.IncreaseFontSize.Execute(null, page._box);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.decrease-font-size-selection-command", Description = "Select all text and invoke DecreaseFontSize command on the RichTextBox.")]
    public static string ProbeDecreaseFontSizeSelectionCommand() => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        page._box.SelectAll();
        WpfDocumentEditingCommands.DecreaseFontSize.Execute(null, page._box);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.apply-font-family-selection-command", Description = "Select all text and invoke ApplyFontFamily command on the RichTextBox.")]
    public static string ProbeApplyFontFamilySelectionCommand(string fontFamily) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        page._box.SelectAll();
        GetEditingCommand("ApplyFontFamily").Execute(new Microsoft.UI.Xaml.Media.FontFamily(fontFamily), page._box);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.apply-foreground-selection-command", Description = "Select all text and invoke ApplyForeground command on the RichTextBox.")]
    public static string ProbeApplyForegroundSelectionCommand() => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        page._box.SelectAll();
        GetEditingCommand("ApplyForeground").Execute(System.Windows.Media.Brushes.LightGreen, page._box);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.apply-background-selection-command", Description = "Select all text and invoke ApplyBackground command on the RichTextBox.")]
    public static string ProbeApplyBackgroundSelectionCommand() => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        page._box.SelectAll();
        GetEditingCommand("ApplyBackground").Execute(System.Windows.Media.Brushes.LightPink, page._box);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    static string ProbeAlignSelectionCommand(System.Windows.Input.RoutedCommand command) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        page._box.SelectAll();
        command.Execute(null, page._box);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.align-left-selection-command", Description = "Select all text and invoke AlignLeft command on the RichTextBox.")]
    public static string ProbeAlignLeftSelectionCommand() =>
        ProbeAlignSelectionCommand(WpfDocumentEditingCommands.AlignLeft);

    [DevFlowAction("richtextbox.probe.align-center-selection-command", Description = "Select all text and invoke AlignCenter command on the RichTextBox.")]
    public static string ProbeAlignCenterSelectionCommand() =>
        ProbeAlignSelectionCommand(WpfDocumentEditingCommands.AlignCenter);

    [DevFlowAction("richtextbox.probe.align-right-selection-command", Description = "Select all text and invoke AlignRight command on the RichTextBox.")]
    public static string ProbeAlignRightSelectionCommand() =>
        ProbeAlignSelectionCommand(WpfDocumentEditingCommands.AlignRight);

    [DevFlowAction("richtextbox.probe.align-justify-selection-command", Description = "Select all text and invoke AlignJustify command on the RichTextBox.")]
    public static string ProbeAlignJustifySelectionCommand() =>
        ProbeAlignSelectionCommand(WpfDocumentEditingCommands.AlignJustify);

    [DevFlowAction("richtextbox.probe.toggle-bullets-selection-command", Description = "Select all text and invoke ToggleBullets command on the RichTextBox.")]
    public static string ProbeToggleBulletsSelectionCommand() => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        page._box.SelectAll();
        WpfDocumentEditingCommands.ToggleBullets.Execute(null, page._box);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.toggle-numbering-selection-command", Description = "Select all text and invoke ToggleNumbering command on the RichTextBox.")]
    public static string ProbeToggleNumberingSelectionCommand() => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        page._box.SelectAll();
        WpfDocumentEditingCommands.ToggleNumbering.Execute(null, page._box);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.increase-indentation-selection-command", Description = "Select all text and invoke IncreaseIndentation command on the RichTextBox.")]
    public static string ProbeIncreaseIndentationSelectionCommand() => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        page._box.SelectAll();
        WpfDocumentEditingCommands.IncreaseIndentation.Execute(null, page._box);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.decrease-indentation-selection-command", Description = "Select all text and invoke DecreaseIndentation command on the RichTextBox.")]
    public static string ProbeDecreaseIndentationSelectionCommand() => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        page._box.SelectAll();
        WpfDocumentEditingCommands.DecreaseIndentation.Execute(null, page._box);
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

    [DevFlowAction("richtextbox.probe.remove-list-markers-command", Description = "Invoke RemoveListMarkers command on the RichTextBox for the current selection.")]
    public static string ProbeRemoveListMarkersCommand() => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.set-list-document first.");

        var removeListMarkersProperty = typeof(WpfDocumentEditingCommands).GetProperty(
            "RemoveListMarkers",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("EditingCommands.RemoveListMarkers not found.");
        var removeListMarkers = (System.Windows.Input.RoutedUICommand)removeListMarkersProperty.GetValue(null)!;
        removeListMarkers.Execute(null, page._box);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.toggle-bullets-command", Description = "Invoke ToggleBullets command on the RichTextBox for the current selection.")]
    public static string ProbeToggleBulletsCommand() => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.set-list-document first.");

        WpfDocumentEditingCommands.ToggleBullets.Execute(null, page._box);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.toggle-numbering-command", Description = "Invoke ToggleNumbering command on the RichTextBox for the current selection.")]
    public static string ProbeToggleNumberingCommand() => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.set-list-document first.");

        WpfDocumentEditingCommands.ToggleNumbering.Execute(null, page._box);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.apply-single-space-selection-command", Description = "Select all text and invoke ApplySingleSpace command on the RichTextBox.")]
    public static string ProbeApplySingleSpaceSelectionCommand() => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        page._box.SelectAll();
        var args = new WpfExecutedRoutedEventArgs(WpfDocumentEditingCommands.AlignLeft, null)
        {
            Source = page._box,
            OriginalSource = page._box,
        };
        InvokeTextEditorParagraphs("OnApplySingleSpace", page._box, args);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.apply-one-and-a-half-space-selection-command", Description = "Select all text and invoke ApplyOneAndAHalfSpace command on the RichTextBox.")]
    public static string ProbeApplyOneAndAHalfSpaceSelectionCommand() => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        page._box.SelectAll();
        var args = new WpfExecutedRoutedEventArgs(WpfDocumentEditingCommands.AlignLeft, null)
        {
            Source = page._box,
            OriginalSource = page._box,
        };
        InvokeTextEditorParagraphs("OnApplyOneAndAHalfSpace", page._box, args);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.apply-double-space-selection-command", Description = "Select all text and invoke ApplyDoubleSpace command on the RichTextBox.")]
    public static string ProbeApplyDoubleSpaceSelectionCommand() => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        page._box.SelectAll();
        var args = new WpfExecutedRoutedEventArgs(WpfDocumentEditingCommands.AlignLeft, null)
        {
            Source = page._box,
            OriginalSource = page._box,
        };
        InvokeTextEditorParagraphs("OnApplyDoubleSpace", page._box, args);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.apply-paragraph-flow-direction-ltr-selection-command", Description = "Select all text and invoke ApplyParagraphFlowDirectionLTR command on the RichTextBox.")]
    public static string ProbeApplyParagraphFlowDirectionLtrSelectionCommand() => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        page._box.SelectAll();
        var args = new WpfExecutedRoutedEventArgs(WpfDocumentEditingCommands.AlignLeft, null)
        {
            Source = page._box,
            OriginalSource = page._box,
        };
        InvokeTextEditorParagraphs("OnApplyParagraphFlowDirectionLTR", page._box, args);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.apply-paragraph-flow-direction-rtl-selection-command", Description = "Select all text and invoke ApplyParagraphFlowDirectionRTL command on the RichTextBox.")]
    public static string ProbeApplyParagraphFlowDirectionRtlSelectionCommand() => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        page._box.SelectAll();
        var args = new WpfExecutedRoutedEventArgs(WpfDocumentEditingCommands.AlignLeft, null)
        {
            Source = page._box,
            OriginalSource = page._box,
        };
        InvokeTextEditorParagraphs("OnApplyParagraphFlowDirectionRTL", page._box, args);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.set-accepts-tab", Description = "Set RichTextBox.AcceptsTab to true or false.")]
    public static string ProbeSetAcceptsTab(bool value) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.AcceptsTab = value;
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.set-accepts-return", Description = "Set RichTextBox.AcceptsReturn to true or false.")]
    public static string ProbeSetAcceptsReturn(bool value) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        page._box.AcceptsReturn = value;
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

    // ── Context Menu Probes ────────────────────────────────────────

    static readonly Lazy<Type> _editorContextMenuType = new(() =>
        typeof(WpfRichTextBox).Assembly.GetType("System.Windows.Documents.TextEditorContextMenu+EditorContextMenu")
        ?? throw new InvalidOperationException("TextEditorContextMenu+EditorContextMenu not found."));

    [DevFlowAction("richtextbox.probe.create-context-menu", Description = "Create an EditorContextMenu and populate it with menu items. Returns item commands and headers.")]
    public static string ProbeCreateContextMenu() => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        var textEditor = RequireTextEditor(page._box);
        var ctor = _editorContextMenuType.Value.GetConstructor(
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic,
            null, [], null)
            ?? throw new InvalidOperationException("EditorContextMenu constructor not found.");
        var menu = ctor.Invoke(null);
        var addMenuItems = _editorContextMenuType.Value.GetMethod("AddMenuItems",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("AddMenuItems not found.");
        addMenuItems.Invoke(menu, [textEditor]);

        // Cast to shim ContextMenu (which extends shim ItemsControl) to
        // reach the shadowed Items property without reflection ambiguity.
        var contextMenu = (System.Windows.Controls.ContextMenu)menu;
        var items = (System.Collections.IList)contextMenu.Items;
        var itemList = new List<string>();
        foreach (var item in items)
        {
            if (item is System.Windows.Controls.MenuItem mi)
            {
                var cmd = mi.Command as System.Windows.Input.RoutedUICommand;
                itemList.Add($"{{\"cmd\":\"{cmd?.Name ?? ""}\",\"header\":\"{mi.Header?.ToString() ?? ""}\"}}");
            }
            else if (item is System.Windows.Controls.Separator)
            {
                itemList.Add($"{{\"type\":\"separator\"}}");
            }
        }
        return $"{{\"itemCount\":{itemList.Count},\"items\":[{string.Join(",", itemList)}]}}";
    });

    [DevFlowAction("richtextbox.probe.clipboard-set-text", Description = "Set the system clipboard to the given plain text without pasting.")]
    public static string ProbeClipboardSetText(string text) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        System.Windows.Clipboard.SetText(text);
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.caret-plain-offset", Description = "Return the plain-text offset of the current caret (selection start).")]
    public static string ProbeCaretPlainOffset() => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created.");
        var document = page._box.Document ?? throw new InvalidOperationException("RichTextBox has no Document.");
        if (page._box.Selection?.Start is not { } start)
            return "{\"offset\":-1}";

        var getOffsetMethod = typeof(WpfRichTextBox).GetMethod("GetPlainTextOffset", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("RichTextBox.GetPlainTextOffset not found.");
        var offset = (int)getOffsetMethod.Invoke(null, [document, start]);
        return $"{{\"offset\":{offset}}}";
    });

    [DevFlowAction("richtextbox.probe.save-xaml", Description = "Save the current document content to XAML and return the XAML string.")]
    public static string ProbeSaveXaml() => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        var document = page._box.Document ?? throw new InvalidOperationException("RichTextBox has no Document.");
        var range = new WpfTextRange(document.ContentStart, document.ContentEnd);

        using var stream = new System.IO.MemoryStream();
        range.Save(stream, System.Windows.DataFormats.Xaml, true);
        stream.Position = 0;
        using var reader = new System.IO.StreamReader(stream);
        var xaml = reader.ReadToEnd();
        return $"{{\"xaml\":{Js(xaml)},\"snapshot\":{Snapshot(page)}}}";
    });

    [DevFlowAction("richtextbox.probe.set-xaml-document", Description = "Replace the current RichTextBox document content by loading XAML via TextRange.Load.")]
    public static string ProbeSetXamlDocument(string xaml) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        var document = page._box.Document ?? throw new InvalidOperationException("RichTextBox has no Document.");
        var range = new WpfTextRange(document.ContentStart, document.ContentEnd);

        using var stream = new System.IO.MemoryStream();
        using var writer = new System.IO.StreamWriter(stream);
        writer.Write(xaml);
        writer.Flush();
        stream.Position = 0;
        range.Load(stream, System.Windows.DataFormats.Xaml);
        page._box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.get-page-count", Description = "Report the number of pages in the current document via FlowDocumentPaginator.")]
    public static string ProbeGetPageCount() => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created.");
        var document = page._box.Document;
        if (document is null)
            return "{\"count\":0}";

        var paginator = ((System.Windows.Documents.IDocumentPaginatorSource)document).DocumentPaginator;
        paginator.PageSize = new Windows.Foundation.Size(640, 100);
        return $"{{\"count\":{paginator.PageCount}}}";
    });

    [DevFlowAction("richtextbox.probe.get-caret-visibility", Description = "Report whether the main caret is visible.")]
    public static string ProbeGetCaretVisibility() => RunOnUi(page =>
    {
        if (page._box is null)
            return "{\"visible\":false}";
        var view = GetInternalProperty(page._box, "RenderScope");
        if (view is null) return "{\"visible\":false}";
        var readOnlyProp = view.GetType().GetProperty("ReadOnly", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        var readOnly = readOnlyProp?.GetValue(view) is true;
        var caretField = view.GetType().GetField("_caret", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (caretField?.GetValue(view) is not Microsoft.UI.Xaml.UIElement caret)
            return "{\"visible\":false}";
        return $"{{\"visible\":{Jb(caret.Visibility == Microsoft.UI.Xaml.Visibility.Visible)},\"readOnly\":{Jb(readOnly)}}}";
    });

    [DevFlowAction("richtextbox.probe.get-drop-caret-visibility", Description = "Report whether the drop caret is visible.")]
    public static string ProbeGetDropCaretVisibility() => RunOnUi(page =>
    {
        if (page._box is null)
            return "{\"visible\":false}";
        var view = GetInternalProperty(page._box, "RenderScope");
        if (view is null) return "{\"visible\":false}";
        var caretField = view.GetType().GetField("_dropCaret", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (caretField?.GetValue(view) is not Microsoft.UI.Xaml.UIElement caret)
            return "{\"visible\":false}";
        return $"{{\"visible\":{Jb(caret.Visibility == Microsoft.UI.Xaml.Visibility.Visible)}}}";
    });

    [DevFlowAction("richtextbox.probe.get-scroll-offset", Description = "Report the vertical scroll offset of the ScrollViewer containing the RichTextBox.")]
    public static string ProbeGetScrollOffset() => RunOnUi(page =>
    {
        if (page._box is null)
            return "{\"offset\":0}";

        // Walk the visual tree up from the RichTextBox to find a ScrollViewer
        var current = (Microsoft.UI.Xaml.DependencyObject?)page._box;
        while (current != null)
        {
            if (current is Microsoft.UI.Xaml.Controls.ScrollViewer sv)
            {
                return $"{{\"offset\":{sv.VerticalOffset}}}";
            }
            current = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(current);
        }
        return "{\"offset\":-1}";
    });

    [DevFlowAction("richtextbox.probe.get-line-count", Description = "Report the number of lines in the current Florence page.")]
    public static string ProbeGetLineCount() => RunOnUi(page =>
    {
        if (page._box is null)
            return "{\"count\":0}";
        var view = GetInternalProperty(page._box, "RenderScope");
        if (view is null) return "{\"count\":0}";
        var pageProp = view.GetType().GetProperty("Page", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        var florencePage = pageProp?.GetValue(view);
        if (florencePage is null) return "{\"count\":0}";
        var linesProp = florencePage.GetType().GetProperty("Lines", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        var lines = linesProp?.GetValue(florencePage) as System.Collections.IList;
        return $"{{\"count\":{lines?.Count ?? 0}}}";
    });

    [DevFlowAction("richtextbox.probe.count-text-changed", Description = "Perform an action and report how many times TextChanged fired. Action: type|paste|toggle-bold|undo|enter.")]
    public static string ProbeCountTextChanged(string action, string text = "") => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created.");

        int count = 0;
        System.Windows.Controls.TextChangedEventHandler handler = (_, _) => count++;
        page._box.TextChanged += handler;

        try
        {
            switch (action)
            {
                case "type":
                    InvokeTextInput(page._box, text.Length > 0 ? text : "x");
                    break;
                case "paste":
                    System.Windows.Clipboard.SetText(text.Length > 0 ? text : "pasted");
                    page._box.Paste();
                    break;
                case "toggle-bold":
                    page._box.SelectAll();
                    WpfDocumentEditingCommands.ToggleBold.Execute(null, page._box);
                    break;
                case "enter":
                    InvokeTextInput(page._box, "\n");
                    break;
            }
            page._box.UpdateLayout();
        }
        finally
        {
            page._box.TextChanged -= handler;
        }

        return $"{{\"count\":{count}}}";
    });

    static void InvokeTextInput(WpfRichTextBox box, string text)
    {
        foreach (var c in text)
        {
            if (char.IsControl(c)) continue;
            var args = new WpfTextCompositionEventArgs(c.ToString()) { OriginalSource = box };
            InvokeTextEditorTyping("OnTextInput", box, args);
        }
    }

    [DevFlowAction("richtextbox.probe.create-large-document", Description = "Create a RichTextBox with N paragraphs of text in one shot.")]
    public static string ProbeCreateLargeDocument(int paragraphCount) => RunOnUi(page =>
    {
        var box = new WpfRichTextBox
        {
            Width = 640,
            Height = 480,
            AcceptsReturn = true,
        };
        var doc = new System.Windows.Documents.FlowDocument();
        for (int i = 0; i < paragraphCount; i++)
        {
            var para = new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run($"paragraph {i} with some text for testing"));
            doc.Blocks.Add(para);
        }
        box.Document = doc;
        page._root.Children.Clear();
        page._box = box;
        page._root.Children.Add(box);
        box.ApplyTemplate();
        box.UpdateLayout();
        return Snapshot(page);
    });

    [DevFlowAction("richtextbox.probe.find-text", Description = "Search for text in the document using TextFindEngine.Find and return the result range as plain-text offsets, or null if not found.")]
    public static string ProbeFindText(string pattern, int flags = 0) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");
        var document = page._box.Document ?? throw new InvalidOperationException("RichTextBox has no Document.");

        var findEngineType = typeof(WpfRichTextBox).Assembly.GetType("System.Windows.Documents.TextFindEngine")
            ?? throw new InvalidOperationException("TextFindEngine not found.");
        var findMethod = findEngineType.GetMethod("Find", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
            ?? throw new InvalidOperationException("TextFindEngine.Find not found.");

        var start = document.ContentStart;
        var end = document.ContentEnd;
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        var result = findMethod.Invoke(null, [start, end, pattern, flags, culture]);

        if (result is null)
            return "{\"found\":false}";

        var range = (System.Windows.Documents.TextRange)result;

        var getOffsetMethod = typeof(WpfRichTextBox).GetMethod("GetPlainTextOffset", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("RichTextBox.GetPlainTextOffset not found.");
        var startOffset = (int)getOffsetMethod.Invoke(null, [document, range.Start]);
        var endOffset = (int)getOffsetMethod.Invoke(null, [document, range.End]);
        var foundText = range.Text;

        return $"{{\"found\":true,\"start\":{startOffset},\"end\":{endOffset},\"text\":{Js(foundText)}}}";
    });

    [DevFlowAction("richtextbox.probe.validate-text-pointer-offsets", Description = "Validate TextPointer offset consistency and return the document's offset range, text length, and round-trip results.")]
    public static string ProbeValidateTextPointerOffsets() => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        var document = page._box.Document ?? throw new InvalidOperationException("RichTextBox has no Document.");
        var start = document.ContentStart;
        var end = document.ContentEnd;
        var offsetToEnd = start.GetOffsetToPosition(end);
        var textLength = new WpfTextRange(start, end).Text?.Length ?? 0;

        // Round-trip test: create positions at progressive fractions of the range
        var roundTrips = new List<string>();
        foreach (var frac in new[] { 0.0, 0.25, 0.5, 0.75, 1.0 })
        {
            int targetOffset = (int)(offsetToEnd * frac);
            var mid = start.GetPositionAtOffset(targetOffset);
            if (mid is not null)
            {
                int actualOffset = start.GetOffsetToPosition(mid);
                roundTrips.Add($"{{\"target\":{targetOffset},\"actual\":{actualOffset},\"match\":{Jb(targetOffset == actualOffset)}}}");
            }
        }

        return $"{{\"offsetToEnd\":{offsetToEnd},\"textLength\":{textLength},\"rangeMatch\":{Jb(offsetToEnd == textLength)},\"roundTrips\":[{string.Join(",", roundTrips)}]}}";
    });

    [DevFlowAction("richtextbox.probe.execute-command", Description = "Execute an ApplicationCommands or EditingCommands command by name (Cut, Copy, Paste, Delete, SelectAll) on the current RichTextBox.")]
    public static string ProbeExecuteCommand(string commandName) => RunOnUi(page =>
    {
        if (page._box is null)
            throw new InvalidOperationException("RichTextBox not created. Call richtextbox.probe.create-plain or richtextbox.probe.set-document first.");

        var command = commandName switch
        {
            "Cut" => System.Windows.Input.ApplicationCommands.Cut,
            "Copy" => System.Windows.Input.ApplicationCommands.Copy,
            "Paste" => System.Windows.Input.ApplicationCommands.Paste,
            "Delete" => System.Windows.Documents.EditingCommands.Delete,
            "SelectAll" => System.Windows.Input.ApplicationCommands.SelectAll,
            "TabForward" => WpfEditingCommands.TabForward,
            "TabBackward" => WpfEditingCommands.TabBackward,
            _ => throw new ArgumentException($"Unknown command: {commandName}", nameof(commandName)),
        };

        if (command.CanExecute(null, page._box))
        {
            command.Execute(null, page._box);
        }

        page._box.UpdateLayout();
        return Snapshot(page);
    });
}
#else
public sealed partial class MainPage : Microsoft.UI.Xaml.Controls.Page
{
}
#endif
