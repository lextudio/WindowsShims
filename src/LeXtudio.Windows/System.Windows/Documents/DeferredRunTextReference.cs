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

    // The value Run.Text should expose: the run's current content text. Reads the backing store
    // directly (via TextRange), never Run.Text, so it cannot re-enter OnTextUpdated.
    internal string Resolve() => new TextRange(_run.ContentStart, _run.ContentEnd).Text ?? string.Empty;
}
