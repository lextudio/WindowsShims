// Placeholder for RubberbandSelector while its bitmap-snapshot rendering stays gated (R8 —
// see docs/fixeddocument/). FixedDocument and FixedDocumentSequence each lazily construct
// one; this satisfies that field/property without providing rubber-band selection.
// Removed automatically once the real RubberbandSelector.cs is un-gated
// (-p:FixedDocumentR8=all), where upstream supplies the type.

namespace System.Windows.Documents
{
    internal sealed class RubberbandSelector
    {
    }
}
