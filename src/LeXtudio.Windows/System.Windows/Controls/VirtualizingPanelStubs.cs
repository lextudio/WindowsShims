using System.Windows.Controls.Primitives;

namespace System.Windows.Controls;

// ScrollUnit: controls whether scroll offsets are in pixels or logical items.
public enum ScrollUnit
{
    Pixel = 0,
    Item  = 1,
}

// VirtualizingPanel: abstract base for WPF virtualizing panels. Extends our
// Panel shim so casts like `panel as VirtualizingStackPanel` compile and work.
public abstract class VirtualizingPanel : Panel
{
    public static readonly DependencyProperty IsVirtualizingProperty =
        DependencyProperty.RegisterAttached("IsVirtualizing", typeof(bool),
            typeof(VirtualizingPanel), new PropertyMetadata(true));

    public static readonly DependencyProperty VirtualizationModeProperty =
        DependencyProperty.RegisterAttached("VirtualizationMode", typeof(VirtualizationMode),
            typeof(VirtualizingPanel), new PropertyMetadata(VirtualizationMode.Standard));

    internal static readonly DependencyProperty ShouldCacheContainerSizeProperty =
        DependencyProperty.RegisterAttached(
            "ShouldCacheContainerSize",
            typeof(bool),
            typeof(VirtualizingPanel),
            new FrameworkPropertyMetadata(true));

    public static ScrollUnit GetScrollUnit(UIElement element) => ScrollUnit.Item;

    public static VirtualizationMode GetVirtualizationMode(DependencyObject element)
        => (VirtualizationMode)element.GetValue(VirtualizationModeProperty);

    public static void SetVirtualizationMode(DependencyObject element, VirtualizationMode value)
        => element.SetValue(VirtualizationModeProperty, value);

    public static bool GetIsVirtualizing(DependencyObject element)
        => (bool)element.GetValue(IsVirtualizingProperty);

    public static void SetIsVirtualizing(DependencyObject element, bool value)
        => element.SetValue(IsVirtualizingProperty, value);

    // IsVirtualizing / InRecyclingMode instance flags live on the linked
    // VirtualizingPanel subclasses (e.g. DataGridCellsPanel) themselves, matching
    // upstream WPF; the attached-DP getters above remain the shared source.

    // WPF VirtualizingPanel.ItemContainerGenerator: the generator scoped to this
    // panel, obtained from the items owner. The linked DataGridCellsPanel measure
    // path drives generation through it.
    protected IItemContainerGenerator? ItemContainerGenerator
        => ItemsControl.GetItemsOwner(this)?.ItemContainerGenerator is IItemContainerGenerator generator
            ? generator.GetItemContainerGeneratorForPanel(this)
            : null;

    // WPF UIElement.MeasureDirty flag, used by BringIndexIntoView retry timing.
    // The Uno layout pass does not expose it; reporting clean keeps the
    // single-pass behavior.
    internal bool MeasureDirty => false;

    // WPF UIElement.MeasureDuringArrange flag. The Uno measure/arrange split does
    // not re-enter measure during arrange, so this is always false.
    internal bool MeasureDuringArrange => false;

    protected override void OnIsItemsHostChanged(bool oldIsItemsHost, bool newIsItemsHost)
    {
    }

    protected virtual void OnClearChildren()
    {
    }

    protected internal virtual void BringIndexIntoView(int index)
    {
    }

    protected virtual void OnItemsChanged(object sender, ItemsChangedEventArgs args)
    {
    }

    protected void AddInternalChild(UIElement child)
        => InternalChildren.Add(child);

    protected void InsertInternalChild(int index, UIElement child)
        => InternalChildren.Insert(index, child);

    protected void RemoveInternalChildRange(int index, int range)
        => RemoveInternalChildRange(InternalChildren, index, range);

    internal static void AddInternalChild(UIElementCollection children, UIElement child)
        => children.Add(child);

    internal static void InsertInternalChild(UIElementCollection children, int index, UIElement child)
        => children.Insert(index, child);

    internal static void RemoveInternalChildRange(UIElementCollection children, int index, int range)
    {
        for (var i = 0; i < range && index < children.Count; i++)
        {
            children.RemoveAt(index);
        }
    }
}

// VirtualizingStackPanel: virtualizes items in a vertical stack.
//
// Session 119 (Slice 4): now that the Panel chain is a live WinUI panel, this is a
// functional row-virtualizing items host. Uno calls MeasureOverride/ArrangeOverride;
// the panel realizes only the rows intersecting the effective viewport (plus a cache
// band) by driving the already-unit-tested VirtualizingRowsRealizer over the owning
// ItemsControl's generator, reports the full scroll extent so the parent ScrollViewer
// scrolls naturally, and recycles off-screen containers. Uniform/estimated row height
// (pixel scrolling) for now; variable heights and BringIndexIntoView arrive later.
//
// The engine is inert until a panel is actually installed as an items host
// (GetItemsOwner returns null otherwise), so it does not affect the current manual
// BuildShimVisualTree render path.
public class VirtualizingStackPanel : VirtualizingPanel
{
    private const int CacheRows = 2;
    private const double DefaultRowHeight = 22d;

    internal bool IgnoreMaxDesiredSize { get; set; }

    private VirtualizingRowsRealizer<UIElement>? _realizer;
    private ItemsControl? _owner;
    private double _viewportTop;
    private double _viewportHeight;
    private bool _hasViewport;
    private double _rowHeight = DefaultRowHeight;
    private Size _lastViewportSize;
    private Microsoft.UI.Xaml.Controls.ScrollViewer? _scrollOwner;

    public VirtualizingStackPanel()
    {
        // Two viewport signals: EffectiveViewportChanged (general) and the ancestor
        // ScrollViewer's ViewChanged (deterministic for a ScrollViewer-hosted panel —
        // the panel is the scroll content, so VerticalOffset is the window top).
        EffectiveViewportChanged += OnEffectiveViewportChanged;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private bool Recycling => GetVirtualizationMode(this) == VirtualizationMode.Recycling;

    private void OnLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_scrollOwner is not null)
            return;

        DependencyObject? current = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(this);
        while (current is not null)
        {
            if (current is Microsoft.UI.Xaml.Controls.ScrollViewer scrollViewer)
            {
                _scrollOwner = scrollViewer;
                // Only a nudge to re-measure; the authoritative viewport comes from
                // EffectiveViewportChanged (see below).
                scrollViewer.ViewChanged += OnScrollViewerViewChanged;
                break;
            }

            current = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(current);
        }
    }

    private void OnUnloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_scrollOwner is not null)
        {
            _scrollOwner.ViewChanged -= OnScrollViewerViewChanged;
            _scrollOwner = null;
        }
    }

    private void OnScrollViewerViewChanged(object? sender, Microsoft.UI.Xaml.Controls.ScrollViewerViewChangedEventArgs e)
        => InvalidateMeasure();

    private void OnEffectiveViewportChanged(
        Microsoft.UI.Xaml.FrameworkElement sender,
        Microsoft.UI.Xaml.EffectiveViewportChangedEventArgs args)
    {
        // EffectiveViewport is the authoritative visible band: it accounts for ALL
        // ancestor clipping/scrolling in the panel's own coordinates, so it stays sane
        // even when the immediate ScrollViewer is measured unbounded (ViewportHeight ==
        // content height). Using ScrollViewer.ViewportHeight here would realize the whole
        // list in that case.
        SetViewport(args.EffectiveViewport.Y, args.EffectiveViewport.Height, args.EffectiveViewport.Width);
    }

    private void SetViewport(double top, double height, double width = double.NaN)
    {
        _viewportTop = top;
        _viewportHeight = height;
        _hasViewport = true;

        var newViewportSize = new Size(double.IsNaN(width) ? _lastViewportSize.Width : width, height);
        if (newViewportSize.Width != _lastViewportSize.Width || newViewportSize.Height != _lastViewportSize.Height)
        {
            OnViewportSizeChanged(_lastViewportSize, newViewportSize);
            _lastViewportSize = newViewportSize;
        }

        InvalidateMeasure();
    }

    // Deterministic test seam: force the realized window to a viewport without relying
    // on async scroll/viewport callbacks. Used by the Roma virtualization probe.
    internal void ShimForceViewport(double top, double height)
        => SetViewport(top, height);

    // Recycle every realized container and re-measure, so the next pass re-prepares the
    // window from scratch. Called when the realization source changes (filter/sort), where
    // the item at a given index may differ from what a still-realized container shows.
    internal void ShimResetRealization()
    {
        _realizer?.Clear();
        InvalidateMeasure();
    }

    private VirtualizingRowsRealizer<UIElement> EnsureRealizer(ItemsControl owner)
    {
        if (_realizer is not null && ReferenceEquals(_owner, owner))
            return _realizer;

        _owner = owner;
        var recycling = Recycling;
        _realizer = new VirtualizingRowsRealizer<UIElement>(
            itemAt: index => owner.Items[index],
            create: item =>
            {
                var container = (UIElement)owner.CreateContainerForItem(item)!;
                InternalChildren.Add(container);
                return container;
            },
            prepare: (container, item, index) =>
            {
                owner.PrepareContainerForItem(container, item);
                owner.ShimOnContainerRealized(container, item, index);
                container.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            },
            clear: (container, item) =>
            {
                owner.ShimOnContainerRecycled(container, item);
                owner.ClearContainerForItem(container, item);
                if (recycling)
                {
                    // Keep recycled containers in the tree (collapsed) for reuse.
                    container.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                }
                else
                {
                    InternalChildren.Remove(container);
                }
            },
            recycling: recycling);
        return _realizer;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var owner = ItemsControl.GetItemsOwner(this);
        if (owner is null)
            return base.MeasureOverride(availableSize);

        var realizer = EnsureRealizer(owner);
        var itemCount = owner.Items.Count;

        // Before the first EffectiveViewportChanged, fall back to the measure
        // constraint so the very first realization shows the top of the list.
        var viewportTop = _hasViewport ? _viewportTop : 0d;
        var viewportHeight = _hasViewport
            ? _viewportHeight
            : (double.IsInfinity(availableSize.Height) ? 0d : availableSize.Height);

        var layout = realizer.Realize(itemCount, _rowHeight, viewportTop, viewportHeight, CacheRows);

        // Measure realized rows; refine the uniform row-height estimate from them.
        var maxWidth = 0d;
        foreach (var entry in realizer.Realized)
        {
            var child = entry.Value;
            child.Measure(new Size(availableSize.Width, double.PositiveInfinity));
            var desired = child.DesiredSize;
            if (desired.Width > maxWidth)
                maxWidth = desired.Width;
            if (desired.Height > 0)
                _rowHeight = desired.Height;
        }

        var width = double.IsInfinity(availableSize.Width) ? maxWidth : Math.Max(maxWidth, availableSize.Width);
        var extent = _rowHeight > 0 ? itemCount * _rowHeight : layout.ExtentHeight;
        return new Size(width, extent);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (_realizer is null)
            return base.ArrangeOverride(finalSize);

        foreach (var entry in _realizer.Realized)
        {
            var child = entry.Value;
            var top = entry.Key * _rowHeight;
            var height = child.DesiredSize.Height > 0 ? child.DesiredSize.Height : _rowHeight;
            child.Arrange(new Rect(0, top, finalSize.Width, height));
        }

        return finalSize;
    }

    // Scroll the row at item index into view (WPF VirtualizingPanel.BringIndexIntoView).
    // Scrolls the owning ScrollViewer to the row's pixel offset AND sets the viewport
    // directly so the next measure realizes the now-visible window synchronously (rather
    // than waiting for the async ScrollViewer.ViewChanged callback).
    protected internal override void BringIndexIntoView(int index)
    {
        if (index < 0)
            return;

        var top = index * _rowHeight;
        _scrollOwner?.ChangeView(null, top, null, true);
        SetViewport(top, _viewportHeight > 0 ? _viewportHeight : DefaultRowHeight * 20);
    }

    protected override void OnIsItemsHostChanged(bool oldIsItemsHost, bool newIsItemsHost)
    {
    }

    protected virtual void OnViewportSizeChanged(Size oldViewportSize, Size newViewportSize)
    {
    }

    protected virtual void OnCleanUpVirtualizedItem(CleanUpVirtualizedItemEventArgs e)
    {
    }
}

public sealed class CleanUpVirtualizedItemEventArgs : EventArgs
{
    public UIElement? UIElement { get; init; }

    public bool Cancel { get; set; }
}
