namespace System.Windows.Controls.Primitives;

// Minimal header shell: carries the column identity the control root and
// reordering paths reference. Visual states, gripper resize, and click
// sorting arrive with the header presenter milestone.
public partial class DataGridColumnHeader : ContentControl
{
    public DataGridColumn? Column { get; internal set; }

    public int DisplayIndex => Column?.DisplayIndex ?? -1;
}
