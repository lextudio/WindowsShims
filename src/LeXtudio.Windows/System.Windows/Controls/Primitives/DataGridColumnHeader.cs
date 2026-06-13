using System.Windows.Controls;

namespace System.Windows.Controls.Primitives;

public partial class DataGridColumnHeader : ContentControl, IProvideDataGridColumn
{
    public DataGridColumn? Column { get; internal set; }

    public int DisplayIndex => Column?.DisplayIndex ?? -1;

    public bool IsVisible => Visibility == Visibility.Visible;

    // Pointer press routes to the owning grid's sort handler (session 30).
    protected override void OnPointerPressed(Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (Column is { DataGridOwner: { } owner } column)
        {
            owner.HandleShimHeaderClicked(column);
        }
    }
}
