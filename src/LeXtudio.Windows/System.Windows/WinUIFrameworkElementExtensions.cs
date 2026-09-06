namespace System.Windows;

public static class WinUIFrameworkElementExtensions
{
    extension(Microsoft.UI.Xaml.UIElement self)
    {
        // WPF UIElement.MoveFocus: keyboard-focus traversal. Always returns false in the shim.
        public bool MoveFocus(Input.TraversalRequest request) => false;
    }

    extension(Microsoft.UI.Xaml.FrameworkElement self)
    {
        public bool IsEnabled => true;

        public bool IsFocused => false;

        public bool IsKeyboardFocused => self == System.Windows.Input.Keyboard.CurrentFocusedElement;

        // WPF property consulted while a logical-tree walk is in progress.
        // We don't model the walk, so it's always false; callers gate stale-tree
        // guards behind this and the false return reads as "not in a walk".
        public bool IsLogicalChildrenIterationInProgress => false;

        // WPF's FrameworkElement.TemplatedParent is public; WinUI/Uno keeps it
        // internal. Upstream WPF source files (e.g. TextSelection.GetParentElement)
        // read it as a parent-walk shortcut and already fall back to
        // VisualTreeHelper.GetParent when null, so returning null here keeps
        // the walk semantics intact without reflecting into Uno internals.
        public DependencyObject? TemplatedParent => null;

    }

    // Static helper since upstream calls `FrameworkElement.GetFrameworkParent(scroller)`.
    extension(Microsoft.UI.Xaml.FrameworkElement)
    {
        public static DependencyObject? GetFrameworkParent(DependencyObject current)
            => Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(current);
    }

    // FrameworkElement.IsEnabledChanged event. C# 14 extension blocks don't allow
    // event declarations, so we expose add/remove methods instead. Upstream
    // TextEditor uses `+= /-=` on this name, which requires a real event — so
    // we provide static helper methods callers can adapt to.
    extension(Microsoft.UI.Xaml.FrameworkElement self)
    {
        public void AddIsEnabledChangedHandler(System.Windows.DependencyPropertyChangedEventHandler handler) { }
        public void RemoveIsEnabledChangedHandler(System.Windows.DependencyPropertyChangedEventHandler handler) { }
    }

    extension(Microsoft.UI.Xaml.UIElement self)
    {
        public bool IsKeyboardFocusWithin => self == System.Windows.Input.Keyboard.CurrentFocusedElement;
        public bool IsEnabled => true;
    }

    // BitmapEffectProperty is obsolete in WinUI; backing field + static extension so
    // upstream code can reference UIElement.BitmapEffectProperty.
    private static readonly DependencyProperty s_bitmapEffectProperty =
        DependencyProperty.Register(
            "BitmapEffect",
            typeof(object),
            typeof(Microsoft.UI.Xaml.UIElement),
            new Microsoft.UI.Xaml.PropertyMetadata(null));

    private static readonly System.Windows.RoutedEvent s_lostFocusEvent = new();

    extension(Microsoft.UI.Xaml.UIElement)
    {
        public static DependencyProperty BitmapEffectProperty => s_bitmapEffectProperty;
        public static System.Windows.RoutedEvent LostFocusEvent => s_lostFocusEvent;
    }

    private static readonly DependencyProperty s_contextMenuProperty =
        DependencyProperty.Register(
            "ContextMenu",
            typeof(System.Windows.Controls.ContextMenu),
            typeof(Microsoft.UI.Xaml.FrameworkElement),
            new Microsoft.UI.Xaml.PropertyMetadata(null));

    private static readonly System.Windows.RoutedEvent s_contextMenuOpeningEvent = new();
    private static readonly System.Windows.RoutedEvent s_contextMenuClosingEvent = new();

    extension(Microsoft.UI.Xaml.FrameworkElement)
    {
        public static DependencyProperty ContextMenuProperty => s_contextMenuProperty;
        public static System.Windows.RoutedEvent ContextMenuOpeningEvent => s_contextMenuOpeningEvent;
        public static System.Windows.RoutedEvent ContextMenuClosingEvent => s_contextMenuClosingEvent;
    }

    extension(Microsoft.UI.Xaml.UIElement self)
    {
        // WPF UIElement.Focusable. WinUI has no equivalent (it uses
        // IsTabStop/FocusState), so the shim reports focusable; placed on
        // UIElement so the linked DataGridHelper.TreeHasFocusAndTabStop resolves
        // it for any UIElement, and FrameworkElement inherits it.
        public bool Focusable { get => true; set { } }
    }

    extension(Microsoft.UI.Xaml.FrameworkElement self)
    {
        public System.Windows.Controls.ContextMenu? ContextMenu
        {
            get => self.GetValue(s_contextMenuProperty) as System.Windows.Controls.ContextMenu;
            set => self.SetValue(s_contextMenuProperty, value);
        }

        public System.Windows.Media.GeneralTransform TransformToDescendant(Microsoft.UI.Xaml.UIElement descendant)
            => new System.Windows.Media.WinUIGeneralTransform(self.TransformToVisual(descendant));

        /// <summary>
        /// WPF's Visual.VisualContentBounds — the bounds of what the element itself draws,
        /// excluding children. WinUI does not separate the two, so the element's own render
        /// size is reported.
        /// </summary>
        public global::Windows.Foundation.Rect VisualContentBounds
            => new(0, 0, self.ActualWidth, self.ActualHeight);

        /// <summary>
        /// WPF's FrameworkElement.ChangeLogicalParent. A no-op, like AddLogicalChild above:
        /// the shim only tracks logical parents for FrameworkContentElement, and a WinUI
        /// FrameworkElement's parenting is owned by Uno's visual tree.
        /// </summary>
        public void ChangeLogicalParent(DependencyObject? newParent) { }
    }

    extension(Microsoft.UI.Xaml.UIElement self)
    {
        /// <summary>
        /// WPF's UIElement.InvalidateVisual — schedules OnRender. WinUI has no OnRender;
        /// invalidating arrange is the nearest request for the element to be redrawn.
        /// </summary>
        public void InvalidateVisual() => self.InvalidateArrange();

        public System.Windows.Media.GeneralTransform TransformToDescendant(Microsoft.UI.Xaml.UIElement descendant)
            => new System.Windows.Media.WinUIGeneralTransform(self.TransformToVisual(descendant));
    }

    extension(Microsoft.UI.Xaml.DependencyObject self)
    {
        /// <summary>
        /// WPF's DependencyObject.InheritanceParent — the object property inheritance flows
        /// from. Uno has no inheritance-context pointer to read, so the visual parent, which
        /// is what inheritance follows in practice, is reported.
        /// </summary>
        public DependencyObject? InheritanceParent
            => self is FrameworkContentElement contentElement
                ? contentElement.Parent
                : Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(self);

        /// <summary>WPF's DependencyObject.DependencyObjectType.</summary>
        public DependencyObjectType DependencyObjectType
            => DependencyObjectType.FromSystemTypeInternal(self.GetType());
    }

    // WPF Visual-tree management APIs that FrameworkElement exposes (via UIElement/Visual).
    // In WinUI, panels manage their own visual children; ToolBarTray (a plain FrameworkElement)
    // calls these in its ToolBarCollection helper. No-ops satisfy the call sites while Uno
    // handles the actual visual parenting through its own visual tree.
    extension(Microsoft.UI.Xaml.FrameworkElement self)
    {
        public void AddVisualChild(Microsoft.UI.Xaml.UIElement child) { }
        public void RemoveVisualChild(Microsoft.UI.Xaml.UIElement child) { }
        public void AddLogicalChild(object child) { }
        public void RemoveLogicalChild(object child) { }

        // WPF SetResourceReference — set a property to a dynamic resource lookup.
        // On Uno, fall back to a simple SetValue(dp, Application.Current.Resources[key]).
        public void SetResourceReference(DependencyProperty dp, object resourceKey)
        {
            if (resourceKey != null &&
                Microsoft.UI.Xaml.Application.Current?.Resources?.TryGetValue(resourceKey, out var res) == true)
                self.SetValue(dp, res);
        }
    }

    extension(Microsoft.UI.Xaml.UIElement self)
    {
        /// <summary>
        /// WPF's UIElement.BringIntoView. WinUI's equivalent is StartBringIntoView,
        /// which scrolls the nearest scrolling host asynchronously.
        /// </summary>
        public void BringIntoView() => self.StartBringIntoView();

        /// <summary>WPF's UIElement.BringIntoView(Rect) — scrolls a sub-rect into view.</summary>
        public void BringIntoView(global::Windows.Foundation.Rect targetRectangle)
            => self.StartBringIntoView(new Microsoft.UI.Xaml.BringIntoViewOptions
            {
                TargetRect = targetRectangle,
            });

        /// <summary>
        /// WPF's VisualTreeHelper.GetDpi(visual), reached unqualified from FrameworkElement
        /// members. Reads the element's XamlRoot rasterization scale; 1.0 before the
        /// element is in a tree.
        /// </summary>
        public Controls.DpiScale GetDpi()
        {
            var scale = self.XamlRoot?.RasterizationScale ?? 1.0;
            return new Controls.DpiScale(scale, scale);
        }
    }

    extension(Microsoft.UI.Xaml.FrameworkElement self)
    {
        /// <summary>
        /// WPF's FrameworkElement.IsInitialized, set once the parser has finished the
        /// element. Always true here: an element handed back by XamlReader.Load is fully
        /// parsed, and Uno's loader has no Initialized event to wait on — reporting false
        /// would send the fixed-document page loader down a path that never completes.
        /// </summary>
        public bool IsInitialized => true;

        /// <summary>
        /// WPF's FrameworkElement.InheritanceBehavior, which controls how far property-value
        /// and resource inheritance walks. WinUI has no equivalent, so the value is stored on
        /// an attached property and read back; it does not change lookup behavior.
        /// </summary>
        public InheritanceBehavior InheritanceBehavior
        {
            get => (InheritanceBehavior)self.GetValue(InheritanceBehaviorHost.InheritanceBehaviorProperty);
            set => self.SetValue(InheritanceBehaviorHost.InheritanceBehaviorProperty, value);
        }
    }
}

/// <summary>
/// WPF's FrameworkElement.InheritanceBehavior values. Recorded but not acted on:
/// WinUI's property system has no inheritance-boundary concept to hook.
/// </summary>
public enum InheritanceBehavior
{
    Default = 0,
    SkipToAppNow,
    SkipToAppNext,
    SkipToThemeNow,
    SkipToThemeNext,
    SkipAllNow,
    SkipAllNext,
}

internal static class InheritanceBehaviorHost
{
    public static readonly DependencyProperty InheritanceBehaviorProperty =
        DependencyProperty.RegisterAttached(
            "InheritanceBehavior",
            typeof(InheritanceBehavior),
            typeof(InheritanceBehaviorHost),
            new Microsoft.UI.Xaml.PropertyMetadata(InheritanceBehavior.Default));
}
