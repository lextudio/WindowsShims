using System.Windows.Documents;

namespace System.Windows.Controls;

/// <summary>WPF SpellCheck shim — forwards IsEnabled changes to the owning control via callback.</summary>
public sealed class SpellCheck
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.Register(nameof(IsEnabled), typeof(bool), typeof(SpellCheck),
            new System.Windows.FrameworkPropertyMetadata(false));

    private readonly Action<bool>? _isEnabledChanged;

    public SpellCheck() { }
    // Legacy overload: owner is a WPF TextBoxBase; no callback needed on that path.
    public SpellCheck(object owner) { }
    public SpellCheck(Action<bool> isEnabledChanged) { _isEnabledChanged = isEnabledChanged; }

    private bool _isEnabled;
    public bool IsEnabled
    {
        get => _isEnabled;
        set { _isEnabled = value; _isEnabledChanged?.Invoke(value); }
    }
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
