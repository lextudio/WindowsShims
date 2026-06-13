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

// Session 59: WinUI TextBox has no GetCharacterIndexFromPoint; the linked
// DataGridTextColumn uses it only to place the caret under the mouse on edit.
// Returning -1 (no hit) makes it fall back to SelectAll, the WPF behavior when
// the click isn't over the text.
public static class WinUITextBoxCaretExtensions
{
    public static int GetCharacterIndexFromPoint(
        this Microsoft.UI.Xaml.Controls.TextBox textBox, Point point, bool snapToText) => -1;
}

// Session 59: the linked DataGridTextColumn drains the dispatcher queue after
// starting an IME edit via Dispatcher.Invoke(Action, DispatcherPriority). The
// generated WinUI Dispatcher (CoreDispatcher) has no such overload; the IME
// drain is unnecessary in the shim, so run the callback synchronously.
public static class WinUIDispatcherInvokeExtensions
{
    public static void Invoke(
        this global::Windows.UI.Core.CoreDispatcher dispatcher,
        Action callback,
        System.Windows.Threading.DispatcherPriority priority) => callback();
}

/// <summary>WPF InputMethod shim — IME not applicable on HAS_UNO.</summary>
public static class InputMethod
{
    public static readonly DependencyProperty IsInputMethodEnabledProperty =
        DependencyProperty.Register("IsInputMethodEnabled", typeof(bool), typeof(InputMethod),
            new System.Windows.FrameworkPropertyMetadata(true));
}
