// Automation peers for the fixed-document family.
//
// LeXtudio.Windows does not implement UI Automation (see the DataGrid section of the
// README): AutomationPeer.FromElement returns null and these peers report nothing. They
// are honest stubs rather than a silent partial implementation, and exist so the linked
// upstream OnCreateAutomationPeer overrides compile.

namespace System.Windows.Automation.Peers;

/// <summary>Peer for a paginated document (FixedDocument, FixedDocumentSequence).</summary>
public class DocumentAutomationPeer : AutomationPeer
{
    // FixedDocument/FixedDocumentSequence derive from FrameworkContentElement, which is not
    // a WinUI FrameworkElement, so the owner cannot be handed to the base peer.
    public DocumentAutomationPeer(object owner) => DocumentOwner = owner;

    public object DocumentOwner { get; }
}

/// <summary>Peer for a single fixed page.</summary>
public class FixedPageAutomationPeer : AutomationPeer
{
    public FixedPageAutomationPeer(Microsoft.UI.Xaml.FrameworkElement owner)
        : base(owner)
    {
    }
}
