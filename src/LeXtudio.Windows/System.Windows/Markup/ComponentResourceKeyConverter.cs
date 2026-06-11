using System.ComponentModel;

namespace System.Windows.Markup
{
    /// <summary>
    /// Compiler shim satisfying the TypeConverter attribute on the linked
    /// ComponentResourceKey. WPF's converter only supports serialization
    /// scenarios that do not apply here.
    /// </summary>
    public class ComponentResourceKeyConverter : TypeConverter
    {
    }
}
