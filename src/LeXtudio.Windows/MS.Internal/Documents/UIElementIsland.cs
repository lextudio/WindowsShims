using Microsoft.UI.Xaml;

namespace MS.Internal.Documents;

public sealed class UIElementIsland : System.IDisposable
{
    public UIElementIsland(UIElement root) => Root = root;
    public UIElement Root { get; }
    public void Dispose()
    {
    }
}
