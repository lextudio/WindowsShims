using System.Windows.Controls;

namespace System.Windows.Controls.Primitives;

public partial class DataGridColumnHeader : ContentControl, IProvideDataGridColumn
{
    public DataGridColumn? Column { get; internal set; }

    public int DisplayIndex => Column?.DisplayIndex ?? -1;

    public bool IsVisible => Visibility == Visibility.Visible;
}
