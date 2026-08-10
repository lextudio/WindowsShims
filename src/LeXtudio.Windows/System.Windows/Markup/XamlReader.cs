// Minimal XAML reader for the WPF document subset produced by
// TextRangeSerialization.WriteXaml (Section → Paragraph → Run / Bold / Italic / ...).
// This is NOT a general-purpose XAML parser — it handles only the known
// document-model elements and attributes that the serializer emits.

using System.IO;
using System.IO.Packaging;
using System.Text;
using System.Xml;
using System.Windows.Documents;

namespace System.Windows.Markup;

public static class XamlReader
{
    static string StripQualifier(string name)
    {
        var dot = name.IndexOf('.');
        return dot >= 0 ? name[(dot + 1)..] : name;
    }

    public static object? Load(XmlReader reader) => Parse(reader);

    public static object? Load(XmlReader reader, bool useRestrictiveXamlXmlReader) => Parse(reader);

    public static object? Load(string xaml)
    {
        // Pre-process: WPF WriteXaml with useFlowDocumentAsRoot=true emits namespace
        // declarations that clash under Uno. Fix common issues before parsing.
        xaml = SanitizeXaml(xaml);
        using var reader = XmlReader.Create(new StringReader(xaml));
        return Parse(reader);
    }

    public static object? Load(Stream stream, ParserContext? parserContext, bool useRestrictiveXamlReader)
    {
        // Copy to MemoryStream first so we can seek freely (OPC package streams may not support it).
        using var memStream = new MemoryStream();
        stream.CopyTo(memStream);
        memStream.Position = 0;

        // Detect OPC package (ZIP-based) vs raw XAML by checking the ZIP magic bytes.
        byte[] magic = new byte[4];
        int read = memStream.Read(magic, 0, 4);
        memStream.Position = 0;
        if (read == 4 && magic[0] == 0x50 && magic[1] == 0x4B && magic[2] == 0x03 && magic[3] == 0x04)
        {
            return LoadFromPackage(memStream);
        }

        using var reader = new StreamReader(memStream);
        var xaml = SanitizeXaml(reader.ReadToEnd());
        using var xmlReader = XmlReader.Create(new StringReader(xaml));
        return Parse(xmlReader);
    }

    static object? LoadFromPackage(Stream stream)
    {
        try
        {
            using (var pkg = System.IO.Packaging.Package.Open(stream, System.IO.FileMode.Open))
            {
                foreach (var part in pkg.GetParts())
                {
                    if ((string)part.ContentType == "application/vnd.ms-wpf.xaml+xml")
                    {
                        using (var partStream = part.GetStream())
                        using (var partReader = new System.IO.StreamReader(partStream))
                        {
                            var xamlText = SanitizeXaml(partReader.ReadToEnd());
                            xamlText = SanitizeXamlPackageXaml(xamlText);
                            return Load(xamlText);
                        }
                    }
                }
            }
        }
        catch
        {
        }
        return null;
    }

    static string SanitizeXaml(string xaml)
    {
        // The WPF XAML serializer emits a duplicate xmlns attribute when the CLR
        // namespace mapping overlaps with Uno's implicit WinUI namespace resolution.
        // Strip it from both raw XAML and OPC package content.
        return SanitizeXamlPackageXaml(xaml);
    }

    static string SanitizeXamlPackageXaml(string xaml)
    {
        const string marker = "Microsoft.UI.Xaml.DependencyProperty";
        int idx;
        while ((idx = xaml.IndexOf(marker, System.StringComparison.Ordinal)) >= 0)
        {
            int start = idx;
            while (start > 0 && xaml[start - 1] != ' ' && xaml[start - 1] != '\t' && xaml[start - 1] != '\n')
                start--;
            if (start > 0) start--;
            int eq = xaml.IndexOf('=', idx + marker.Length);
            if (eq < 0) break;
            int quote = xaml.IndexOf('\"', eq + 1);
            if (quote < 0) break;
            int endQuote = xaml.IndexOf('\"', quote + 1);
            if (endQuote < 0) break;
            xaml = xaml[..start] + xaml[(endQuote + 1)..];
        }
        return xaml;
    }

    static object? Parse(XmlReader reader)
    {
        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                switch (reader.LocalName)
                {
                    case "Section":
                        return ParseSection(reader);
                    case "FlowDocument":
                        return ParseFlowDocument(reader);
                    case "Span":
                        return ParseSpan(reader);
                }
            }
        }
        return null;
    }

    static Section ParseFlowDocument(XmlReader reader)
    {
        // WpfPayload.SaveRange uses useFlowDocumentAsRoot=true, producing
        // <FlowDocument> as root. Extract its blocks into a Section.
        var section = new Section();
        int depth = reader.Depth;
        while (reader.Read() && reader.Depth > depth)
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                var block = ParseBlock(reader);
                if (block is not null)
                    section.Blocks.Add(block);
            }
        }
        return section;
    }

    static Section ParseSection(XmlReader reader)
    {
        var section = new Section();
        int depth = reader.Depth;
        while (reader.Read() && reader.Depth > depth)
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                var block = ParseBlock(reader);
                if (block is not null)
                    section.Blocks.Add(block);
            }
        }
        return section;
    }

    static Block? ParseBlock(XmlReader reader)
    {
        switch (reader.LocalName)
        {
            case "Paragraph":
                return ParseParagraph(reader);
            case "List":
                return ParseList(reader);
            case "Table":
                return ParseTable(reader);
            default:
                ConsumeUnknownElement(reader);
                return null;
        }
    }

    static Paragraph ParseParagraph(XmlReader reader)
    {
        var para = new Paragraph();
        // RTF->XAML emits whole-paragraph formatting (FontSize, FontFamily,
        // FontWeight, FontStyle, ...) as attributes on <Paragraph>; WPF flows these
        // to runs via DP inheritance. The shim applies them as local values so they
        // survive save/load (ITextPointer.GetValue re-derives them for the runs).
        while (reader.MoveToNextAttribute())
        {
            var attrName = StripQualifier(reader.LocalName);
            switch (attrName)
            {
                case "FontSize":
                case "FontFamily":
                case "FontWeight":
                case "FontStyle":
                case "Foreground":
                case "Background":
                    ApplyInlineProperty(para, attrName, reader.Value);
                    break;
                case "TextAlignment":
                    if (Enum.TryParse<TextAlignment>(reader.Value, out var alignment))
                        para.TextAlignment = alignment;
                    break;
                case "FlowDirection":
                    if (Enum.TryParse<FlowDirection>(reader.Value, out var flowDirection))
                        para.FlowDirection = flowDirection;
                    break;
                case "Margin":
                    if (TryParseThickness(reader.Value, out var margin))
                        para.Margin = margin;
                    break;
                case "TextIndent":
                    if (double.TryParse(reader.Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var indent))
                        para.TextIndent = indent;
                    break;
                case "LineHeight":
                    if (double.TryParse(reader.Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var lineHeight))
                        para.LineHeight = lineHeight;
                    break;
                case "BorderThickness":
                    if (TryParseThickness(reader.Value, out var borderThickness))
                        para.BorderThickness = borderThickness;
                    break;
                case "BorderBrush":
                    if (TryParseColor(reader.Value, out var borderColor))
                        para.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(borderColor);
                    break;
                case "Language":
                case "xml:lang":
                case "lang":
                    if (!string.IsNullOrWhiteSpace(reader.Value))
                        para.SetValue(Microsoft.UI.Xaml.FrameworkElement.LanguageProperty, reader.Value);
                    break;
            }
        }
        reader.MoveToElement();
        var textBuffer = new StringBuilder();
        void FlushText()
        {
            if (textBuffer.Length == 0)
                return;
            var text = textBuffer.ToString();
            textBuffer.Clear();
            if (string.IsNullOrWhiteSpace(text))
                return;
            para.Inlines.Add(new Run(text));
        }
        int depth = reader.Depth;
        while (reader.Read() && reader.Depth > depth)
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                FlushText();
                var inline = ParseInline(reader);
                if (inline is not null)
                    para.Inlines.Add(inline);
            }
            else if (reader.NodeType == XmlNodeType.Text || reader.NodeType == XmlNodeType.SignificantWhitespace)
            {
                textBuffer.Append(reader.Value);
            }
        }
        FlushText();
        return para;
    }

    static Inline? ParseInline(XmlReader reader)
    {
        switch (reader.LocalName)
        {
            case "Run":
                return ParseRun(reader);
            case "Bold":
                return ParseBold(reader);
            case "Italic":
                return ParseItalic(reader);
            case "Underline":
                return ParseUnderline(reader);
            case "Hyperlink":
                return ParseHyperlink(reader);
            case "Span":
                return ParseSpanInline(reader);
            case "LineBreak":
                // Leave the reader on the element; the caller's reader.Read()
                // advances to the following sibling.
                return new LineBreak();
            default:
                ConsumeUnknownElement(reader);
                return null;
        }
    }

    // Consume an unknown element subtree, leaving the reader positioned at the
    // element's end tag (or on the element itself when empty), so the caller's
    // next reader.Read() advances to the following sibling element.
    static void ConsumeUnknownElement(XmlReader reader)
    {
        if (reader.IsEmptyElement)
            return;
        int depth = reader.Depth;
        reader.Read();
        while (reader.Depth > depth)
            reader.Read();
    }

    static Run ParseRun(XmlReader reader)
    {
        var run = new Run();
        string? text = null;
        while (reader.MoveToNextAttribute())
        {
            var attrName = StripQualifier(reader.LocalName);
            if (attrName == "Text")
                text = reader.Value;
            else
                ApplyInlineProperty(run, attrName, reader.Value);
        }
        reader.MoveToElement();
        // Consume <Run>text</Run> and end positioned AT its </Run> end tag, so the
        // caller's next reader.Read() advances to the following sibling element.
        // (ReadElementContentAsString must NOT be used: it leaves the reader
        // positioned on the next sibling, causing that element to be skipped.)
        if (text is null && !reader.IsEmptyElement)
        {
            reader.Read();
            if (reader.NodeType == XmlNodeType.Text || reader.NodeType == XmlNodeType.SignificantWhitespace)
            {
                text = reader.Value;
                reader.Read();
            }
            while (reader.NodeType != XmlNodeType.EndElement)
                reader.Read();
        }
        // For <Run Text="..."/> self-closing elements, leave the reader positioned
        // on the element itself so the caller's reader.Read() reaches the next sibling.
        if (text is not null)
        {
            run.Text = text;
        }
        return run;
    }

    static void ApplyInlineProperty(TextElement element, string attrName, string value)
    {
        switch (attrName)
        {
            case "FontWeight":
                if (value.Equals("Bold", StringComparison.OrdinalIgnoreCase))
                    element.FontWeight = FontWeights.Bold;
                else if (value.Equals("Normal", StringComparison.OrdinalIgnoreCase))
                    element.FontWeight = FontWeights.Normal;
                else if (ushort.TryParse(value, out var w))
                    element.FontWeight = new FontWeight { Weight = w };
                break;
            case "FontStyle":
                if (value.Equals("Italic", StringComparison.OrdinalIgnoreCase))
                    element.FontStyle = FontStyles.Italic;
                else if (value.Equals("Oblique", StringComparison.OrdinalIgnoreCase))
                    element.FontStyle = FontStyles.Oblique;
                break;
            case "FontSize":
                var fsText = value.Trim();
                double pt = 1.0;
                if (fsText.EndsWith("pt", StringComparison.OrdinalIgnoreCase))
                {
                    fsText = fsText[..^2].Trim();
                    pt = 96.0 / 72.0;
                }
                else if (fsText.EndsWith("px", StringComparison.OrdinalIgnoreCase))
                {
                    fsText = fsText[..^2].Trim();
                }
                if (double.TryParse(fsText, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var fs) && fs > 0)
                    element.FontSize = fs * pt;
                break;
            case "FontFamily":
                if (!string.IsNullOrWhiteSpace(value))
                    element.FontFamily = new Microsoft.UI.Xaml.Media.FontFamily(value);
                break;
            case "Foreground":
                if (TryParseColor(value, out var color))
                    element.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(color);
                break;
            case "Background":
                if (TryParseColor(value, out var bg))
                    element.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(bg);
                break;
            case "TextDecorations":
                var decorations = ParseTextDecorations(value);
                if (decorations is not null)
                    element.SetValue(Inline.TextDecorationsProperty, decorations);
                break;
            case "FlowDirection":
                if (element is Inline inline && Enum.TryParse<FlowDirection>(value, out var flowDirection))
                    inline.FlowDirection = flowDirection;
                break;
            case "Variants":
                // RtfToXamlReader emits super/subscript as <Span
                // Typography.Variants="Superscript|Subscript">; StripQualifier has
                // already reduced the attribute name to "Variants". Carry it on the
                // element so WriteXaml can serialize it back to \super / \sub.
                if (Enum.TryParse<FontVariants>(value, out var variants))
                    element.SetValue(System.Windows.Documents.Typography.VariantsProperty, variants);
                break;
            case "Language":
            case "xml:lang":
            case "lang":
                // WriteXaml serializes FrameworkElement.LanguageProperty (an
                // inheritable property) as a "Language" attribute, while
                // RtfToXamlReader re-emits it as xml:lang="<culture>". Both map to
                // the same WinUI language property so the value survives RTF
                // save/load as \langN (XmlReader.LocalName already dropped the
                // "xml:" prefix, hence the extra "lang" arm).
                if (!string.IsNullOrWhiteSpace(value))
                    element.SetValue(Microsoft.UI.Xaml.FrameworkElement.LanguageProperty, value);
                break;
        }
    }

    // Parse a TextDecorations attribute value ("Underline", "Strikethrough", or a
    // comma-separated combination such as "Underline, Strikethrough" as emitted by
    // RtfToXamlReader). Reuses the TextDecoration instances from the TextDecorations
    // singletons so reference-based equality (HasUnderline in the test host) holds.
    static System.Windows.Media.TextDecorationCollection? ParseTextDecorations(string value)
    {
        System.Windows.Media.TextDecorationCollection? result = null;
        foreach (var part in value.Split(','))
        {
            var name = part.Trim();
            System.Windows.Media.TextDecorationCollection? collection = name.Equals("Underline", StringComparison.OrdinalIgnoreCase) ? System.Windows.Media.TextDecorations.Underline
                : name.Equals("Strikethrough", StringComparison.OrdinalIgnoreCase) ? System.Windows.Media.TextDecorations.Strikethrough
                : name.Equals("Overline", StringComparison.OrdinalIgnoreCase) ? System.Windows.Media.TextDecorations.Overline
                : name.Equals("Baseline", StringComparison.OrdinalIgnoreCase) ? System.Windows.Media.TextDecorations.Baseline
                : null;
            if (collection is null)
                continue;
            result ??= new System.Windows.Media.TextDecorationCollection();
            foreach (var decoration in collection)
                result.Add(decoration);
        }
        return result;
    }

    // Parse a comma-separated "left,top,right,bottom" Margin value into a Thickness.
    // Also accepts the uniform single-value form ("1") like WPF's ThicknessConverter.
    static bool TryParseThickness(string value, out Thickness thickness)
    {
        thickness = new Thickness();
        var parts = value.Split(',');
        if (parts.Length != 1 && parts.Length != 4)
            return false;
        var values = new double[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            if (!double.TryParse(parts[i], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out values[i]))
                return false;
        }
        thickness = parts.Length == 1
            ? new Thickness(values[0])
            : new Thickness(values[0], values[1], values[2], values[3]);
        return true;
    }

    static Bold ParseBold(XmlReader reader)
    {
        var bold = new Bold();
        // Bold is a marker element; carry the formatting as a local property value
        // so WriteXaml (which reduces it to <Span>) can serialize FontWeight.
        bold.FontWeight = FontWeights.Bold;
        PopulateSpan(bold, reader);
        return bold;
    }

    static Italic ParseItalic(XmlReader reader)
    {
        var italic = new Italic();
        italic.FontStyle = FontStyles.Italic;
        PopulateSpan(italic, reader);
        return italic;
    }

    static Underline ParseUnderline(XmlReader reader)
    {
        var underline = new Underline();
        underline.SetValue(Inline.TextDecorationsProperty, System.Windows.Media.TextDecorations.Underline);
        PopulateSpan(underline, reader);
        return underline;
    }

    static Hyperlink ParseHyperlink(XmlReader reader)
    {
        var hyperlink = new Hyperlink();
        string? uri = null;
        while (reader.MoveToNextAttribute())
        {
            if (reader.LocalName == "NavigateUri")
                uri = reader.Value;
        }
        reader.MoveToElement();
        if (Uri.TryCreate(uri, UriKind.Absolute, out var navUri))
            hyperlink.NavigateUri = navUri;
        PopulateSpan(hyperlink, reader);
        return hyperlink;
    }

    static Span ParseSpanInline(XmlReader reader)
    {
        var span = new Span();
        while (reader.MoveToNextAttribute())
            ApplyInlineProperty(span, StripQualifier(reader.LocalName), reader.Value);
        reader.MoveToElement();
        PopulateSpan(span, reader);
        return span;
    }

    static Span ParseSpan(XmlReader reader)
    {
        var span = new Span();
        PopulateSpan(span, reader);
        return span;
    }

    static void PopulateSpan(Span span, XmlReader reader)
    {
        var textBuffer = new StringBuilder();
        void FlushText()
        {
            if (textBuffer.Length == 0)
                return;
            var text = textBuffer.ToString();
            textBuffer.Clear();
            if (string.IsNullOrWhiteSpace(text))
                return;
            span.Inlines.Add(new Run(text));
        }
        int depth = reader.Depth;
        while (reader.Read() && reader.Depth > depth)
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                FlushText();
                var inline = ParseInline(reader);
                if (inline is not null)
                    span.Inlines.Add(inline);
            }
            else if (reader.NodeType == XmlNodeType.Text || reader.NodeType == XmlNodeType.SignificantWhitespace)
            {
                textBuffer.Append(reader.Value);
            }
        }
        FlushText();
    }

    static List ParseList(XmlReader reader)
    {
        var list = new List();
        while (reader.MoveToNextAttribute())
        {
            switch (StripQualifier(reader.LocalName))
            {
                case "MarkerStyle":
                    if (Enum.TryParse<TextMarkerStyle>(reader.Value, out var markerStyle))
                        list.MarkerStyle = markerStyle;
                    break;
                case "StartIndex":
                    if (int.TryParse(reader.Value, out var startIndex))
                        list.StartIndex = startIndex;
                    break;
            }
        }
        reader.MoveToElement();
        int depth = reader.Depth;
        while (reader.Read() && reader.Depth > depth)
        {
            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "ListItem")
            {
                var li = new ListItem();
                int itemDepth = reader.Depth;
                while (reader.Read() && reader.Depth > itemDepth)
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        var block = ParseBlock(reader);
                        if (block is not null)
                            li.Blocks.Add(block);
                    }
                }
                list.ListItems.Add(li);
            }
        }
        return list;
    }

    static Table ParseTable(XmlReader reader)
    {
        var table = new Table();
        while (reader.MoveToNextAttribute())
        {
            var attrName = StripQualifier(reader.LocalName);
            switch (attrName)
            {
                case "Background":
                    if (TryParseColor(reader.Value, out var tableBg))
                        table.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(tableBg);
                    break;
            }
        }
        reader.MoveToElement();
        int depth = reader.Depth;
        TableRowGroup? currentRg = null;
        TableRow? currentRow = null;
        while (reader.Read() && reader.Depth > depth)
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                switch (reader.LocalName)
                {
                    case "TableColumn":
                        var column = new TableColumn();
                        while (reader.MoveToNextAttribute())
                        {
                            if (StripQualifier(reader.LocalName) == "Width" &&
                                double.TryParse(reader.Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var width))
                                column.Width = new System.Windows.GridLength(width);
                        }
                        reader.MoveToElement();
                        table.Columns.Add(column);
                        break;
                    case "Table.Columns":
                        // Complex-property wrapper emitted by WriteXaml; its
                        // <TableColumn> children are handled by the case above.
                        break;
                    case "TableRowGroup":
                        currentRg = new TableRowGroup();
                        while (reader.MoveToNextAttribute())
                        {
                            if (StripQualifier(reader.LocalName) == "Background" && TryParseColor(reader.Value, out var rgBg))
                                currentRg.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(rgBg);
                        }
                        reader.MoveToElement();
                        table.RowGroups.Add(currentRg);
                        break;
                    case "TableRow":
                        currentRow = new TableRow();
                        while (reader.MoveToNextAttribute())
                        {
                            if (StripQualifier(reader.LocalName) == "Background" && TryParseColor(reader.Value, out var rowBg))
                                currentRow.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(rowBg);
                        }
                        reader.MoveToElement();
                        currentRg?.Rows.Add(currentRow);
                        break;
                    case "TableCell":
                        var cell = new TableCell();
                        while (reader.MoveToNextAttribute())
                        {
                            var attrName = StripQualifier(reader.LocalName);
                            switch (attrName)
                            {
                                case "ColumnSpan":
                                    if (int.TryParse(reader.Value, out var columnSpan))
                                        cell.ColumnSpan = columnSpan;
                                    break;
                                case "RowSpan":
                                    if (int.TryParse(reader.Value, out var rowSpan))
                                        cell.RowSpan = rowSpan;
                                    break;
                                case "Padding":
                                    if (TryParseThickness(reader.Value, out var padding))
                                        cell.Padding = padding;
                                    break;
                                case "BorderThickness":
                                    if (TryParseThickness(reader.Value, out var borderThickness))
                                        cell.BorderThickness = borderThickness;
                                    break;
                                case "BorderBrush":
                                    if (TryParseColor(reader.Value, out var borderColor))
                                        cell.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(borderColor);
                                    break;
                                case "Background":
                                    if (TryParseColor(reader.Value, out var cellBg))
                                        cell.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(cellBg);
                                    break;
                            }
                        }
                        reader.MoveToElement();
                        int cellDepth = reader.Depth;
                        while (reader.Read() && reader.Depth > cellDepth)
                        {
                            if (reader.NodeType == XmlNodeType.Element)
                            {
                                var block = ParseBlock(reader);
                                if (block is not null)
                                    cell.Blocks.Add(block);
                            }
                        }
                        currentRow?.Cells.Add(cell);
                        break;
                }
            }
        }
        return table;
    }

static bool TryParseColor(string value, out global::Windows.UI.Color color)
{
    color = default;
    if (string.IsNullOrEmpty(value)) return false;
    if (value[0] == '#')
    {
        if (value.Length == 7 && uint.TryParse(value[1..], System.Globalization.NumberStyles.HexNumber, null, out var rgb))
        {
            color = global::Windows.UI.Color.FromArgb(255, (byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
            return true;
        }
        if (value.Length == 9 && uint.TryParse(value[1..], System.Globalization.NumberStyles.HexNumber, null, out var argb))
        {
            color = global::Windows.UI.Color.FromArgb((byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb);
            return true;
        }
    }
        // Named colors (basic set)
        return value.ToLowerInvariant() switch
        {
            "black" => TryParseColor("#FF000000", out color),
            "white" => TryParseColor("#FFFFFFFF", out color),
            "red" => TryParseColor("#FFFF0000", out color),
            "blue" => TryParseColor("#FF0000FF", out color),
            "green" => TryParseColor("#FF008000", out color),
            "gray" or "grey" => TryParseColor("#FF808080", out color),
            "transparent" => TryParseColor("#00FFFFFF", out color),
            _ => false,
        };
    }
}
