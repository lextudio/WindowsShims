using System.Collections;
using System.Collections.Specialized;

namespace System.Windows.Controls;

// WPF-shaped virtuals that Selector/MultiSelector override. WPF declares these
// across UIElement/FrameworkElement/ItemsControl; the shim hosts them all on
// ItemsControl because the Uno base types do not have them. They are no-op
// hooks until item-container generation exists.
public partial class ItemsControl
{
    protected virtual void OnInitialized(EventArgs e)
    {
    }

    protected virtual void OnIsKeyboardFocusWithinChanged(DependencyPropertyChangedEventArgs e)
    {
    }

    protected virtual void OnItemsChanged(NotifyCollectionChangedEventArgs e)
    {
    }

    protected virtual void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
    {
    }

    protected virtual void PrepareContainerForItemOverride(DependencyObject element, object item)
    {
    }

    protected virtual void ClearContainerForItemOverride(DependencyObject element, object item)
    {
    }

    internal virtual void AdjustItemInfoOverride(NotifyCollectionChangedEventArgs e)
    {
    }
}
