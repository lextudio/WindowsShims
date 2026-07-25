// Minimal XAML reader for the WPF document subset produced by
// TextRangeSerialization.WriteXaml (Section → Paragraph → Run / Bold / Italic / ...).
// This is NOT a general-purpose XAML parser — it handles only the known
// document-model elements and attributes that the serializer emits.

using System.IO;
using System.IO.Packaging;
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
                reader.Skip();
                return null;
        }
    }

    static Paragraph ParseParagraph(XmlReader reader)
    {
        var para = new Paragraph();
        int depth = reader.Depth;
        while (reader.Read() && reader.Depth > depth)
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                var inline = ParseInline(reader);
                if (inline is not null)
                    para.Inlines.Add(inline);
            }
        }
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
                reader.Skip();
                return new LineBreak();
            default:
                reader.Skip();
                return null;
        }
    }

    static Run ParseRun(XmlReader reader)
    {
        var run = new Run();
        string? text = null;
        while (reader.MoveToNextAttribute())
        {
            var attrName = StripQualifier(reader.LocalName);
            switch (attrName)
            {
                case "Text": text = reader.Value; break;
                case "FontWeight":
                    if (reader.Value.Equals("Bold", StringComparison.OrdinalIgnoreCase))
                        run.FontWeight = FontWeights.Bold;
                    else if (ushort.TryParse(reader.Value, out var w))
                        run.FontWeight = new FontWeight { Weight = w };
                    break;
                case "FontStyle":
                    if (reader.Value.Equals("Italic", StringComparison.OrdinalIgnoreCase))
                        run.FontStyle = FontStyles.Italic;
                    else if (reader.Value.Equals("Oblique", StringComparison.OrdinalIgnoreCase))
                        run.FontStyle = FontStyles.Oblique;
                    break;
                case "FontSize":
                    if (double.TryParse(reader.Value, out var fs) && fs > 0)
                        run.FontSize = fs;
                    break;
                case "Foreground":
                    if (TryParseColor(reader.Value, out var color))
                        run.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(color);
                    break;
                case "Background":
                    if (TryParseColor(reader.Value, out var bg))
                        run.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(bg);
                    break;
                case "TextDecorations":
                    if (reader.Value.Equals("Underline", StringComparison.OrdinalIgnoreCase))
                        run.SetValue(Inline.TextDecorationsProperty, System.Windows.Media.TextDecorations.Underline);
                    else if (reader.Value.Equals("Strikethrough", StringComparison.OrdinalIgnoreCase))
                        run.SetValue(Inline.TextDecorationsProperty, System.Windows.Media.TextDecorations.Strikethrough);
                    break;
            }
        }
        reader.MoveToElement();
        // Read text content if present between elements
        if (text is null)
        {
            text = reader.ReadElementContentAsString();
        }
        else
        {
            reader.Skip();
        }
        if (text is not null)
        {
            run.Text = text;
        }
        return run;
    }

    static Bold ParseBold(XmlReader reader)
    {
        var bold = new Bold();
        PopulateSpan(bold, reader);
        return bold;
    }

    static Italic ParseItalic(XmlReader reader)
    {
        var italic = new Italic();
        PopulateSpan(italic, reader);
        return italic;
    }

    static Underline ParseUnderline(XmlReader reader)
    {
        var underline = new Underline();
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
        int depth = reader.Depth;
        while (reader.Read() && reader.Depth > depth)
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                var inline = ParseInline(reader);
                if (inline is not null)
                    span.Inlines.Add(inline);
            }
        }
    }

    static List ParseList(XmlReader reader)
    {
        var list = new List();
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
        int depth = reader.Depth;
        TableRowGroup? currentRg = null;
        TableRow? currentRow = null;
        while (reader.Read() && reader.Depth > depth)
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                switch (reader.LocalName)
                {
                    case "TableRowGroup":
                        currentRg = new TableRowGroup();
                        table.RowGroups.Add(currentRg);
                        break;
                    case "TableRow":
                        currentRow = new TableRow();
                        currentRg?.Rows.Add(currentRow);
                        break;
                    case "TableCell":
                        var cell = new TableCell();
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
