// System.Windows.Shapes types that have WinUI equivalents are forwarded as thin subclasses.
// This lets WPF source files with `using System.Windows.Shapes;` compile unchanged.

namespace System.Windows.Shapes;

public partial class Path : Microsoft.UI.Xaml.Shapes.Path { }
