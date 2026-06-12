namespace System.Windows.Automation.Peers
{
    // Mirrors WPF's member order.
    public enum AutomationEvents
    {
        ToolTipOpened,
        ToolTipClosed,
        MenuOpened,
        MenuClosed,
        AutomationFocusChanged,
        InvokePatternOnInvoked,
        SelectionItemPatternOnElementAddedToSelection,
        SelectionItemPatternOnElementRemovedFromSelection,
        SelectionItemPatternOnElementSelected,
        SelectionPatternOnInvalidated,
        TextPatternOnTextSelectionChanged,
        TextPatternOnTextChanged,
        AsyncContentLoaded,
        PropertyChanged,
        StructureChanged,
        LiveRegionChanged,
        Notification,
        ActiveTextPositionChanged,
    }
}
