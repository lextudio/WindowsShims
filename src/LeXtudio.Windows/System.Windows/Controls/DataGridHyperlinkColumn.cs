namespace System.Windows.Controls;

// Session 121 (DataGrid gap survey item 7): real, working hyperlink cell,
// replacing the former placeholder (`new TextBlock()`, no binding, no click).
//
// Real WPF's GenerateElement builds a TextBlock+Hyperlink+InlineUIContainer
// chain relying on WPF's inline-run hit-testing to route clicks. This shim's
// plain TextBlock (System.Windows.Controls.TextBlock) doesn't route pointer
// events through Inlines on this Uno target — only RichTextBox's Florence
// engine (MS.Internal.Documents.FlowDocumentView) does that; a bare
// Inlines.Add(new Hyperlink()) here would compile but never receive a click.
// So this renders a plain (already-proven-live-bindable, same mechanism
// DataGridTextColumn uses) TextBlock styled to look like a hyperlink, and
// handles Tapped directly instead of routing through Hyperlink.RequestNavigate.
public class DataGridHyperlinkColumn : DataGridBoundColumn
{
    // WPF's Hyperlink.TargetName (frame/window navigation target) — kept for
    // source/API parity with code that sets it; unused, since this shim has no
    // frame/window-targeting concept to route it to.
    public string? TargetName { get; set; }

    // WPF's DataGridHyperlinkColumn.ContentBinding: an optional separate
    // binding for the *displayed* text. Falls back to Binding itself (the
    // bound Uri's own ToString()) when not set, matching upstream.
    public System.Windows.Data.BindingBase? ContentBinding { get; set; }

    protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
    {
        // Microsoft.UI.Xaml.Controls.TextBlock has no TextDecorations property
        // (that's only meaningful on Run/Florence-engine text, see the class
        // comment above), so the underline is rendered via an Underline inline
        // instead. This is display-only: Tapped is wired on textBlock itself,
        // not routed through Inlines.
        var textBlock = new TextBlock
        {
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                global::Windows.UI.Color.FromArgb(0xFF, 0x00, 0x66, 0xCC)),
        };
        var underline = new System.Windows.Documents.Underline();
        var run = new System.Windows.Documents.Run();
        underline.Inlines.Add(run);
        textBlock.Inlines.Add(underline);

        ApplyStyle(isEditing: false, defaultToElementStyle: false, textBlock);

        // No separate ContentBinding → display Binding's own value (typically a Uri)
        // as text. TextBlock.Text is string-typed and this shim's binding engine
        // does not auto-stringify a non-string source value the way real WPF's
        // default value-conversion step would, so wrap with an explicit ToString()
        // converter — same effect ("show the Uri's text"), just spelled out.
        var displayBinding = ContentBinding ?? StringifyBinding(Binding);
        if (displayBinding is not null)
        {
            System.Windows.Data.BindingOperations.SetBinding(run, System.Windows.Documents.Run.TextProperty, displayBinding);
        }

        textBlock.Tapped += (_, _) => NavigateToBoundUri(dataItem);
        return textBlock;
    }

    protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
        => GenerateElement(cell, dataItem);

    private static System.Windows.Data.BindingBase? StringifyBinding(System.Windows.Data.BindingBase? binding)
    {
        if (binding is not System.Windows.Data.Binding source || source.Converter is not null)
        {
            return binding;
        }

        return new System.Windows.Data.Binding(source.Path?.Path ?? "")
        {
            Converter = ToStringConverter.Instance,
        };
    }

    private sealed class ToStringConverter : System.Windows.Data.IValueConverter
    {
        internal static readonly ToStringConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
            => value?.ToString();

        public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
            => throw new NotSupportedException();
    }

    // Resolves the navigation target directly from the column's own Binding
    // against this row's data item — a one-shot BindingExpression evaluation
    // (the same technique System.Windows.Data.PropertyGroupDescription.
    // GroupNameFromItem uses), rather than requiring a live bound control
    // property. Launches via Windows.System.Launcher.LaunchUriAsync, the same
    // "open URL" mechanism already proven working for RichTextBox hyperlinks
    // (MS.Internal.Documents.FlowDocumentView.ActivateHyperlink).
    private void NavigateToBoundUri(object dataItem)
    {
        if (Binding is not System.Windows.Data.Binding binding)
        {
            return;
        }

        Uri? uri = null;
        try
        {
            var expression = (System.Windows.Data.BindingExpression)
                System.Windows.Data.BindingExpression.CreateUntargetedBindingExpression(null!, binding);
            expression.Activate(dataItem);
            uri = expression.Value as Uri;
            expression.Deactivate();
        }
        catch
        {
            // Non-fatal: an unresolvable/malformed binding just means no navigation.
        }

        if (uri is not null)
        {
            _ = global::Windows.System.Launcher.LaunchUriAsync(uri);
        }
    }
}
