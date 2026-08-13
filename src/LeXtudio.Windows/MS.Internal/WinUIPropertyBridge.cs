#if WINDOWS_APP_SDK
namespace MS.Internal;

/// <summary>
/// Reconciles WPF's "any DependencyProperty on any DependencyObject" model with WinAppSDK,
/// which binds a natively-declared DependencyProperty to its owner type.
/// </summary>
/// <remarks>
/// WPF looks DependencyProperties up globally, and Uno's lookup is permissive, so the WPF
/// sources freely read properties off objects whose type is unrelated to the declaring one.
/// WinAppSDK rejects that with E_UNEXPECTED (0x8000FFFF). Only natively-declared WinUI
/// properties are affected — the shim's own registrations are attached, so they work on any
/// object. Measured against the live property set, 54 of 56 inheritable/behavioral properties
/// transfer fine; only the two named below are a problem.
/// </remarks>
internal static class WinUIPropertyBridge
{
    /// <summary>
    /// True when <paramref name="property"/> can be read from / written to a FlowDocument.
    /// </summary>
    /// <remarks>
    /// In WPF a FlowDocument is a FrameworkContentElement, so Language and AllowDrop exist on
    /// it and RichTextBox transfers them to its implicit document. The shim's FlowDocument is a
    /// plain DependencyObject, so those two are skipped: they describe the hosting element, not
    /// the document's text formatting.
    /// </remarks>
    internal static bool IsTransferableToDocument(Microsoft.UI.Xaml.DependencyProperty property)
        => !ReferenceEquals(property, Microsoft.UI.Xaml.FrameworkElement.LanguageProperty)
        && !ReferenceEquals(property, Microsoft.UI.Xaml.UIElement.AllowDropProperty)
        // TableColumn.BackgroundProperty is Panel.BackgroundProperty.AddOwner(...), and the
        // shim's AddOwner returns the same instance because WinUI has no equivalent — so this
        // is literally the Panel-owned property. Reading it from a TableColumn (a plain
        // DependencyObject) does not throw: it access-violates and kills the process.
        && !ReferenceEquals(property, Microsoft.UI.Xaml.Controls.Panel.BackgroundProperty);

    /// <summary>
    /// Reads the editor's UiScope background, routing to the property owner that applies.
    /// </summary>
    /// <remarks>
    /// TextSelection.GetCaretBrush reads Panel.BackgroundProperty off the UiScope, which for a
    /// RichTextBox is a Control rather than a Panel. Under WinAppSDK that throws, and because
    /// the caret update runs on a dispatcher callback the exception is unhandled and takes the
    /// process down rather than surfacing as a test failure.
    /// </remarks>
    internal static object? GetUiScopeBackground(Microsoft.UI.Xaml.DependencyObject? uiScope)
        => uiScope switch
        {
            Microsoft.UI.Xaml.Controls.Control control
                => control.GetValue(Microsoft.UI.Xaml.Controls.Control.BackgroundProperty),
            Microsoft.UI.Xaml.Controls.Panel panel
                => panel.GetValue(Microsoft.UI.Xaml.Controls.Panel.BackgroundProperty),
            _ => null,
        };
}
#endif
