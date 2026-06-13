namespace System.Windows.Controls.Primitives;

// Minimal presenter shell: the control root only needs the type and owner
// notification entry point until header generation exists.
public partial class DataGridColumnHeadersPresenter : ItemsControl
{
    internal DataGrid? ParentDataGrid { get; set; }

    internal ContainerTracking<DataGridColumnHeader>? HeaderTrackingRoot { get; set; }

    internal void NotifyPropertyChanged(
        DependencyObject d,
        string propertyName,
        DependencyPropertyChangedEventArgs e,
        DataGridNotificationTarget target)
    {
    }
}
