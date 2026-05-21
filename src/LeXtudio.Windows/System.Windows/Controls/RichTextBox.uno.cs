#if HAS_UNO
namespace System.Windows.Controls;

public partial class RichTextBox
{
    internal static Action<string>? Logger;

    private static readonly string _logPath =
        System.IO.Path.Combine(System.IO.Path.GetTempPath(), "rtb-template.log");

    private static void Log(string msg)
    {
        Logger?.Invoke($"[RichTextBox] {msg}");
        System.Diagnostics.Debug.WriteLine($"[RichTextBox] {msg}");
        try { System.IO.File.AppendAllText(_logPath,
            $"{DateTime.Now:HH:mm:ss.fff}  [RichTextBox] {msg}\n"); } catch { }
    }

    protected override void InitializeDefaultStyleKey()
    {
        DefaultStyleKey = typeof(RichTextBox);
        Log($"InitializeDefaultStyleKey: set to typeof(RichTextBox)");
    }

    protected override void OnApplyTemplate()
    {
        Log($"OnApplyTemplate: DefaultStyleKey={DefaultStyleKey}, Template={Template}");
        try
        {
            base.OnApplyTemplate();
            Log($"OnApplyTemplate: done, Template={Template}");
        }
        catch (Exception ex)
        {
            Log($"OnApplyTemplate THREW: {ex.GetType().Name}: {ex.Message}");
            Log($"  StackTrace: {ex.StackTrace?.Split('\n')[0]}");
        }
    }
}
#endif
