using System.Collections;
using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace System.Windows.Data;

// Bridge subset of WPF's BindingGroup for the DataGrid row-validation
// surface. Real binding-group semantics (proposed values, item tracking,
// transactional commit across expressions) require the WPF property engine;
// the bridge stores state and reports edits as committable.
public partial class BindingGroup : DependencyObject
{
    public Collection<ValidationRule> ValidationRules { get; } = [];

    public IList Items { get; } = new Collection<object?>();

    public string? Name { get; set; }

    public bool NotifyOnValidationError { get; set; }

    public bool SharesProposedValues { get; set; }

    public void BeginEdit()
    {
    }

    public bool CommitEdit() => true;

    public bool ValidateWithoutUpdate() => true;

    public void CancelEdit()
    {
    }
}
