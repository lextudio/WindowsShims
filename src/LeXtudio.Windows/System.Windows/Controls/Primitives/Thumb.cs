namespace System.Windows.Controls.Primitives;

public partial class Thumb : Control
{
    private const string ThumbTemplateXaml =
        "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' " +
        "xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>" +
        "<Border Background='{TemplateBinding Background}' />" +
        "</ControlTemplate>";

    private static Microsoft.UI.Xaml.Controls.ControlTemplate? _thumbTemplate;

    public static readonly RoutedEvent DragStartedEvent = EventManager.RegisterRoutedEvent(
        "DragStarted", RoutingStrategy.Bubble, typeof(DragStartedEventHandler), typeof(Thumb));

    public static readonly RoutedEvent DragDeltaEvent = EventManager.RegisterRoutedEvent(
        "DragDelta", RoutingStrategy.Bubble, typeof(DragDeltaEventHandler), typeof(Thumb));

    public static readonly RoutedEvent DragCompletedEvent = EventManager.RegisterRoutedEvent(
        "DragCompleted", RoutingStrategy.Bubble, typeof(DragCompletedEventHandler), typeof(Thumb));

    private static readonly RoutedEvent MouseDoubleClickEvent = EventManager.RegisterRoutedEvent(
        "MouseDoubleClick", RoutingStrategy.Bubble, typeof(Input.MouseButtonEventHandler), typeof(Thumb));

    private double _lastX, _lastY;
    private bool _isDragging;

    public event DragStartedEventHandler DragStarted
    {
        add => AddHandler(DragStartedEvent, value);
        remove => RemoveHandler(DragStartedEvent, value);
    }

    public event DragDeltaEventHandler DragDelta
    {
        add => AddHandler(DragDeltaEvent, value);
        remove => RemoveHandler(DragDeltaEvent, value);
    }

    public event DragCompletedEventHandler DragCompleted
    {
        add => AddHandler(DragCompletedEvent, value);
        remove => RemoveHandler(DragCompletedEvent, value);
    }

    public event Input.MouseButtonEventHandler MouseDoubleClick
    {
        add => AddHandler(MouseDoubleClickEvent, value);
        remove => RemoveHandler(MouseDoubleClickEvent, value);
    }

    public Thumb()
    {
        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
        PointerCaptureLost += OnPointerCaptureLost;
        PointerEntered += OnPointerEntered;
        PointerExited += OnPointerExited;
        DoubleTapped += OnDoubleTapped;
    }

    internal bool HasShimCursor => ProtectedCursor is not null;

    protected override void InitializeDefaultStyleKey()
    {
        if (_thumbTemplate == null)
        {
            _thumbTemplate = (Microsoft.UI.Xaml.Controls.ControlTemplate)
                Microsoft.UI.Xaml.Markup.XamlReader.Load(ThumbTemplateXaml);
        }

        Template = _thumbTemplate;
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        ApplyHeaderGripperCursor();
    }

    private void OnPointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        var pt = e.GetCurrentPoint(this);
        if (!pt.Properties.IsLeftButtonPressed)
            return;

        var position = StablePointerPosition(e);
        _lastX = position.X;
        _lastY = position.Y;
        _isDragging = true;
        CapturePointer(e.Pointer);
        e.Handled = true;

        RaiseEvent(new DragStartedEventArgs(_lastX, _lastY)
        {
            RoutedEvent = DragStartedEvent,
            Source = this,
        });
    }

    private void OnPointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (!_isDragging)
            return;

        var position = StablePointerPosition(e);
        var deltaX = position.X - _lastX;
        var deltaY = position.Y - _lastY;
        _lastX = position.X;
        _lastY = position.Y;
        e.Handled = true;

        RaiseEvent(new DragDeltaEventArgs(deltaX, deltaY)
        {
            RoutedEvent = DragDeltaEvent,
            Source = this,
        });
    }

    private void OnPointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (!_isDragging)
            return;

        _isDragging = false;
        ReleasePointerCapture(e.Pointer);
        e.Handled = true;

        RaiseEvent(new DragCompletedEventArgs(0, 0, false)
        {
            RoutedEvent = DragCompletedEvent,
            Source = this,
        });
    }

    private void OnPointerCaptureLost(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (!_isDragging)
            return;

        _isDragging = false;
        RaiseEvent(new DragCompletedEventArgs(0, 0, true)
        {
            RoutedEvent = DragCompletedEvent,
            Source = this,
        });
    }

    private void OnPointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        ApplyHeaderGripperCursor();
    }

    private void OnPointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (IsHeaderGripper && !_isDragging)
        {
            ProtectedCursor = null;
        }
    }

    private void OnDoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        RaiseEvent(new Input.MouseButtonEventArgs
        {
            RoutedEvent = MouseDoubleClickEvent,
            Source = this,
            ClickCount = 2,
        });
        e.Handled = true;
    }

    private bool IsHeaderGripper =>
        Name is "PART_LeftHeaderGripper" or "PART_RightHeaderGripper";

    private void ApplyHeaderGripperCursor()
    {
        if (IsHeaderGripper && ProtectedCursor is null)
        {
            ProtectedCursor = Microsoft.UI.Input.InputSystemCursor.Create(
                Microsoft.UI.Input.InputSystemCursorShape.SizeWestEast);
        }
    }

    private global::Windows.Foundation.Point StablePointerPosition(Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        var root = XamlRoot?.Content as Microsoft.UI.Xaml.UIElement;
        return e.GetCurrentPoint(root ?? this).Position;
    }
}
