namespace System.Windows.Controls;

/// <summary>
/// WPF ToolTip shim. MetaDataGrid uses it for hover-over cell tooltips; full popup
/// rendering is deferred to the Roma.Host UI milestone.
/// </summary>
public class ToolTip : ContentControl
{
    public bool IsOpen { get; set; }
    public object PlacementTarget { get; set; }
    public event RoutedEventHandler Closed;

    public void Close()
    {
        IsOpen = false;
        Closed?.Invoke(this, new RoutedEventArgs());
    }
}
