namespace System.Windows.Documents;

public partial class Hyperlink
{
    internal void RaiseClick() => OnClick();

    internal void RaiseClickForUno() => RaiseClick();
}
