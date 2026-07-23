namespace System.Windows.Documents;

// Upstream Run.OnTextUpdated stores one of these in Run.TextProperty as a lazy proxy for the
// run's content text (a WPF perf optimization that avoids duplicating the string). Real WPF
// resolves the proxy inside DependencyProperty.GetValue; Uno's property system has no such hook
// and would hand the raw proxy back, so the (string) cast in Run.Text throws. To stay compatible
// we keep a reference to the run and resolve eagerly in FrameworkContentElement.SetCurrentDeferredValue,
// storing the materialized string instead of the proxy.
internal sealed class DeferredRunTextReference
{
    private readonly Run _run;

    internal DeferredRunTextReference(Run run) => _run = run;

    // The value Run.Text should expose: the run's current content text. Matches upstream WPF's
    // DeferredRunTextReference.GetValue, which reads via TextRangeBase.GetTextInternal instead of
    // constructing a TextRange. GetTextInternal walks TextPointers directly and never calls
    // TextRangeBase.Select/TextRangeEditTables.BuildTableRange, so it stays safe to call from
    // inside a reposition (e.g. TextRangeEditLists.MergeParagraphs) where a Run's TextElement.Parent
    // chain is transiently inconsistent under this Uno shim.
    internal string Resolve() => TextRangeBase.GetTextInternal(_run.ContentStart, _run.ContentEnd) ?? string.Empty;
}
