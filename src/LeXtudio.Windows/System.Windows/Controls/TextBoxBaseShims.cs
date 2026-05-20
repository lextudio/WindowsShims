using System.Windows.Documents;

namespace System.Windows.Controls;

/// <summary>Stub for WPF SpellCheck — no spell-checking on HAS_UNO.</summary>
public sealed class SpellCheck
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.Register(nameof(IsEnabled), typeof(bool), typeof(SpellCheck),
            new System.Windows.FrameworkPropertyMetadata(false));

    public SpellCheck() { }
    public SpellCheck(object owner) { }

    public bool IsEnabled { get; set; }
    public SpellingReform SpellingReform { get; set; }
    public System.Collections.IList CustomDictionaries { get; } = new System.Collections.ArrayList();
}

/// <summary>WPF InputMethod shim — IME not applicable on HAS_UNO.</summary>
public static class InputMethod
{
    public static readonly DependencyProperty IsInputMethodEnabledProperty =
        DependencyProperty.Register("IsInputMethodEnabled", typeof(bool), typeof(InputMethod),
            new System.Windows.FrameworkPropertyMetadata(true));
}
