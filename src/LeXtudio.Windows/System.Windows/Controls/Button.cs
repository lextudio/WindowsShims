using System.Windows.Controls.Primitives;

namespace System.Windows.Controls;

public class Button : ButtonBase
{
    public Button()
    {
        DefaultStyleKey = typeof(Button);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        UpdateVisualState(false);
    }
}
