#if WINUI_BRIDGE
using System;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System.Windows.Input;

namespace System.Windows
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public sealed class AttachedPropertyBrowsableForTypeAttribute : Attribute
    {
        public AttachedPropertyBrowsableForTypeAttribute(Type targetType)
        {
            TargetType = targetType;
        }

        public Type TargetType { get; }
    }

    public abstract class ContentPosition
    {
    }

    public sealed class LocalValueEnumerator
    {
        public bool MoveNext() => false;
        public LocalValueEntry Current => default;
        public void Reset() { }
        public int Count => 0;
    }

    // BindingExpressionBase consolidated into System.Windows.Data
    // (System.Windows/Data/BindingExpression.cs); the System.Windows copy
    // shadowed the bridge via enclosing-namespace lookup.


    public readonly struct LocalValueEntry
    {
        public DependencyProperty Property { get; init; }

        public object? Value { get; init; }
    }

    public class ResourceDictionary : System.Collections.Generic.Dictionary<object, object?>
    {
    }

    public delegate void DragEventHandler(object sender, DragEventArgs e);
    public delegate void QueryContinueDragEventHandler(object sender, QueryContinueDragEventArgs e);
    public delegate void GiveFeedbackEventHandler(object sender, GiveFeedbackEventArgs e);

    public enum DragAction
    {
        Continue = 0,
        Drop = 1,
        Cancel = 2,
    }

    public class QueryContinueDragEventArgs : RoutedEventArgs
    {
        public DragAction Action { get; set; }
        public DragDropKeyStates KeyStates { get; set; }
        public bool EscapePressed { get; set; }
    }

    public class GiveFeedbackEventArgs : RoutedEventArgs
    {
        public DragDropEffects Effects { get; set; }
        public bool UseDefaultCursors { get; set; } = true;
    }

    public class DragEventArgs : RoutedEventArgs
    {
        public IDataObject Data { get; set; }
        public DragDropEffects AllowedEffects { get; set; }
        public DragDropEffects Effects { get; set; }
        public DragDropKeyStates KeyStates { get; set; }
        public Point GetPosition(object relativeTo) => default;
    }

    [Flags]
    public enum DragDropEffects
    {
        None  = 0,
        Copy  = 1,
        Move  = 2,
        Link  = 4,
        Scroll = unchecked((int)0x80000000),
        All   = Copy | Move | Link,
    }

    [Flags]
    public enum DragDropKeyStates
    {
        None        = 0,
        LeftMouseButton  = 1,
        RightMouseButton = 2,
        ShiftKey    = 4,
        ControlKey  = 8,
        MiddleMouseButton = 16,
        AltKey      = 32,
    }

    public static class DragDrop
    {
        public static readonly RoutedEvent DragEnterEvent    = new RoutedEvent("DragEnter",    typeof(DragEventHandler));
        public static readonly RoutedEvent DragLeaveEvent    = new RoutedEvent("DragLeave",    typeof(DragEventHandler));
        public static readonly RoutedEvent DragOverEvent     = new RoutedEvent("DragOver",     typeof(DragEventHandler));
        public static readonly RoutedEvent DropEvent         = new RoutedEvent("Drop",         typeof(DragEventHandler));
        public static readonly RoutedEvent QueryContinueDragEvent = new RoutedEvent("QueryContinueDrag", typeof(QueryContinueDragEventHandler));
        public static readonly RoutedEvent GiveFeedbackEvent = new RoutedEvent("GiveFeedback", typeof(GiveFeedbackEventHandler));
        public static readonly RoutedEvent PreviewDragEnterEvent = new RoutedEvent("PreviewDragEnter", typeof(DragEventHandler));
        public static readonly RoutedEvent PreviewDragLeaveEvent = new RoutedEvent("PreviewDragLeave", typeof(DragEventHandler));
        public static readonly RoutedEvent PreviewDragOverEvent  = new RoutedEvent("PreviewDragOver",  typeof(DragEventHandler));
        public static readonly RoutedEvent PreviewDropEvent      = new RoutedEvent("PreviewDrop",      typeof(DragEventHandler));

        public static DragDropEffects DoDragDrop(DependencyObject dragSource, IDataObject dataObject, DragDropEffects allowedEffects)
            => DragDropEffects.None;
    }

    public delegate void DependencyPropertyChangedEventHandler(object sender, System.Windows.DependencyPropertyChangedEventArgs e);
}

namespace System.Windows.Documents
{
}

namespace System.Windows.Input
{
    public class RoutedUICommand : RoutedCommand
    {
        public string Text { get; }

        public RoutedUICommand(string text, string name, Type ownerType)
            : base(name, ownerType)
        {
            Text = text ?? string.Empty;
        }

        public RoutedUICommand(string text, string name, Type ownerType, InputGestureCollection inputGestures)
            : base(name, ownerType, inputGestures)
        {
            Text = text ?? string.Empty;
        }
    }

    public class KeyboardFocusChangedEventArgs : RoutedEventArgs
    {
        public Microsoft.UI.Xaml.UIElement? NewFocus { get; init; }
        public Microsoft.UI.Xaml.UIElement? OldFocus { get; init; }
    }

    public class Cursor
    {
    }

    public enum InputScopeNameValue
    {
        Default = 0,
        Url = 1,
        FullFilePath = 2,
        FileName = 3,
        EmailUserName = 4,
        EmailSmtpAddress = 5,
        LogOnName = 6,
        PersonalFullName = 7,
        PersonalNamePrefix = 8,
        PersonalGivenName = 9,
        PersonalMiddleName = 10,
        PersonalSurname = 11,
        PersonalNameSuffix = 12,
        PostalAddress = 13,
        PostalCode = 14,
        AddressStreet = 15,
        AddressStateOrProvince = 16,
        AddressCity = 17,
        AddressCountryName = 18,
        AddressCountryShortName = 19,
        CurrencyAmountAndSymbol = 20,
        CurrencyAmount = 21,
        Date = 22,
        DateMonth = 23,
        DateDay = 24,
        DateYear = 25,
        DateMonthName = 26,
        DateDayName = 27,
        Digits = 28,
        Number = 29,
        OneChar = 30,
        Password = 31,
        TelephoneNumber = 32,
        TelephoneCountryCode = 33,
        TelephoneAreaCode = 34,
        TelephoneLocalNumber = 35,
        Time = 36,
        TimeHour = 37,
        TimeMinorSec = 38,
        NumberFullWidth = 39,
        AlphanumericHalfWidth = 40,
        AlphanumericFullWidth = 41,
        CurrencyChinese = 42,
        Bopomofo = 43,
        Hiragana = 44,
        KatakanaHalfWidth = 45,
        KatakanaFullWidth = 46,
        Hanja = 47,
        PhraseList = -1,
        RegularExpression = -2,
        Srgs = -3,
        Xml = -4,
    }

    public class InputScopeName
    {
        public InputScopeName() { }
        public InputScopeName(InputScopeNameValue nameValue) { NameValue = nameValue; }
        public InputScopeNameValue NameValue { get; set; }
    }

    public class InputScopePhrase
    {
        public InputScopePhrase() { }
        public InputScopePhrase(string name) { Name = name; }
        public string Name { get; set; } = string.Empty;
    }

    public class InputScope
    {
        public System.Collections.Generic.IList<InputScopeName> Names { get; } = new System.Collections.Generic.List<InputScopeName>();
        public System.Collections.Generic.IList<InputScopePhrase> PhraseList { get; } = new System.Collections.Generic.List<InputScopePhrase>();
        public string? RegularExpression { get; set; }
        public string? SrgsMarkup { get; set; }
    }
}

namespace System.Windows.Threading
{
    public struct DispatcherProcessingDisabled : IDisposable
    {
        public void Dispose()
        {
        }
    }
}

namespace MS.Win32
{
    internal static class NativeMethods
    {
        internal const int LOCALE_FONTSIGNATURE = 0x0058;
        internal const int S_OK    = 0x00000000;
        internal const int S_FALSE = 0x00000001;
        internal const int E_OUTOFMEMORY = unchecked((int)0x8007000E);
    }

    internal static class SafeNativeMethods
    {
        internal const ushort C1_SPACE = 0x0008;
        internal const ushort C1_BLANK = 0x0040;
        internal const uint   CT_CTYPE1 = 0x00000001;
        internal const uint   CT_CTYPE3 = 0x00000004;
        internal const ushort C1_PUNCT      = 0x0010;
        internal const ushort C3_KATAKANA   = 0x0020;
        internal const ushort C3_HIRAGANA   = 0x0040;
        internal const ushort C3_IDEOGRAPH  = 0x0100;
        internal const ushort C3_HALFWIDTH  = 0x0400;
        internal const ushort C3_FULLWIDTH  = 0x0800;
        internal const ushort C3_DIACRITIC  = 0x0002;
        internal const ushort C3_NONSPACING = 0x0001;
        internal const ushort C3_VOWELMARK  = 0x0004;
        internal const ushort C3_KASHIDA    = 0x0040;

        internal static int GetKeyboardLayoutList(int nBuff, IntPtr[]? lpList) => 0;

        internal static bool GetStringTypeEx(uint locale, uint dwInfoType, ReadOnlySpan<char> lpSrcStr, Span<ushort> lpCharType) => false;
        internal static int ShowCursor(bool show) => 0;
        internal static bool IsWindowEnabled(System.Runtime.InteropServices.HandleRef hwnd) => true;
    }

    internal static partial class UnsafeNativeMethods
    {
        internal static int GetLocaleInfoW(int locale, int lcType, string lpLCData, int cchData) => 0;
        internal static bool SetForegroundWindow(System.Runtime.InteropServices.HandleRef hwnd) => false;

        internal static int FindNLSString(int locale, uint dwFindNLSStringFlags, ReadOnlySpan<char> lpStringSource, ReadOnlySpan<char> lpStringValue, out int foundLength)
        {
            foundLength = 0;
            return -1;
        }

        // Overload matching WPF's P/Invoke signature used by TextFindEngine.FindNLSString.
        internal static int FindNLSString(int locale, uint dwFindNLSStringFlags, string lpStringSource, int cchSource, string lpStringValue, int cchValue, out int foundLength)
        {
            foundLength = 0;
            return -1;
        }

        [System.Runtime.InteropServices.ComImport]
        [System.Runtime.InteropServices.InterfaceType(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
        [System.Runtime.InteropServices.Guid("fde1eaee-6924-4cdf-91e7-da38cff5559d")]
        public interface ITfInputScope
        {
            void GetInputScopes(out IntPtr ppinputscopes, out int count);
            [System.Runtime.InteropServices.PreserveSig]
            int GetPhrase(out IntPtr ppbstrPhrases, out int count);
            [System.Runtime.InteropServices.PreserveSig]
            int GetRegularExpression([System.Runtime.InteropServices.Out, System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.BStr)] out string desc);
            [System.Runtime.InteropServices.PreserveSig]
            int GetSRGC([System.Runtime.InteropServices.Out, System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.BStr)] out string desc);
            [System.Runtime.InteropServices.PreserveSig]
            int GetXML([System.Runtime.InteropServices.Out, System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.BStr)] out string desc);
        }
    }
}

namespace System.Windows.Media
{
    public class GlyphRun
    {
    }
}

namespace System.Windows
{
    // Stub for WPF binding expression type used in undo guards
    internal abstract class Expression
    {
    }
}

namespace System.Windows.Documents
{
    // TextEditor stub removed in Session 16; upstream TextEditor.cs is now active.

    internal sealed partial class FormattingDependencyObject : DependencyObject
    {
    }

    // Stub: TSF host registration is not modeled in the shim layer.
    internal sealed class TextServicesHost
    {
        public static TextServicesHost? Current => null;
        public static void StartTransitoryExtension(object? textStore) { }
        public static void StopTransitoryExtension(object? textStore) { }
    }

    // AdornerLayer: upstream AdornerLayer.cs enabled in Session 25.
    // CaretElement: remains deferred (see CaretElement.cs Compile Remove); stub satisfies
    // TextSelection.cs, ITextSelection.cs, and TextEditorDragDrop.cs type references.
    internal sealed class CaretElement
    {
        internal const double c_endOfParaMagicMultiplier = 1.0;
        internal const double CaretPaddingWidth = 1.0;

        internal CaretElement(TextEditor textEditor, bool isBlinkEnabled) { }

        internal bool IsSelectionActive { get; set; }
        internal Geometry SelectionGeometry { get; set; }

        internal static FrameworkElement GetOwnerElement(DependencyObject uiScope)
            => uiScope as FrameworkElement;

        internal void SetBlinking(bool isBlinkEnabled) { }
        internal void OnTextViewUpdated() { }
        internal void Hide() { }
        internal void RefreshCaret(bool italic) { }

        internal void Update(bool visible, Rect caretRect, Brush caretBrush, double opacity, bool italic, CaretScrollMethod scrollMethod, double scrollToOriginPosition) { }

        internal void UpdateSelection() { }
        internal void DetachFromView() { }
    }


    internal sealed class TextStore
    {
        internal TextStore(TextEditor textEditor)
        {
        }

        internal bool IsComposing => false;
        internal bool IsInterimSelection => false;

        internal void OnAttach() { }
        internal void OnDetach(bool finalizer) { }
        internal void OnLayoutUpdated() { }
        internal void OnGotFocus() { }
        internal void OnLostFocus() { }
        internal void OnSelectionChange() { }
        internal void OnSelectionChanged() { }
        internal void CompleteComposition() { }
        internal void CompleteCompositionAsync() { }

        internal bool QueryRangeOrReconvertSelection(bool fDoReconvert)
        {
            return false;
        }

        internal void UpdateCompositionText(object composition) { }

        internal object GetReconversionCandidateList()
        {
            return null;
        }
    }

    internal sealed class ImmComposition
    {
        internal static ImmComposition GetImmComposition(DependencyObject uiScope)
        {
            return null;
        }

        internal bool IsComposition => false;

        internal void OnGotFocus(TextEditor editor) { }
        internal void OnLostFocus() { }
        internal void OnDetach(TextEditor editor) { }
        internal void OnLayoutUpdated() { }
        internal void OnSelectionChange() { }
        internal void OnSelectionChanged() { }
        internal void UpdateCompositionText(object composition) { }
        internal void CompleteComposition() { }
    }

    public enum SpellingReform
    {
        PreAndPostreform,
        Prereform,
        Postreform,
    }

    public sealed class SpellingError
    {
        internal ITextPointer? Start => null;
        internal ITextPointer? End => null;
        public System.Collections.Generic.IEnumerable<string> Suggestions => System.Linq.Enumerable.Empty<string>();
        public void IgnoreAll() { }
    }

    internal sealed class Speller
    {
        internal Speller(TextEditor editor)
        {
        }

        internal void Detach() { }
        internal void SetCustomDictionaries(object dictionarySources, bool add) { }
        internal void SetSpellingReform(SpellingReform spellingReform) { }
        internal SpellingError GetError(ITextPointer position, LogicalDirection direction, bool forceEvaluation) => null;
        internal ITextPointer GetNextSpellingErrorPosition(ITextPointer position, LogicalDirection direction) => null;
    }

    internal abstract class ShutDownListener
    {
        protected ShutDownListener(object target, ShutDownEvents events)
        {
        }

        internal abstract void OnShutDown(object target, object sender, EventArgs e);

        internal void StopListening() { }
    }

    [Flags]
    internal enum ShutDownEvents
    {
        DomainUnload = 1,
        DispatcherShutdown = 2,
    }

    // UndoState is defined in upstream MS.Internal.Documents.UndoManager.

    // TextEditorMouse stub removed in Session 17; upstream TextEditorMouse.cs is now active.

    // TextEditorTyping stub removed in Session 18; upstream TextEditorTyping.cs is now active.
    // TextEditorSelection stub removed in Session 18; upstream TextEditorSelection.cs is now active.

    // TextEditorLists stub removed in Session 17; upstream TextEditorLists.cs is now active.
    // TextEditorParagraphs stub removed in Session 17; upstream TextEditorParagraphs.cs is now active.

    // TextEditorCopyPaste stub removed in Session 19; upstream TextEditorCopyPaste.cs is now active.
    // TextEditorContextMenu stub removed in Session 17; upstream TextEditorContextMenu.cs is now active.

    // TextEditorSpelling stub removed in Session 26; upstream TextEditorSpelling.cs is now active.

    // TextEditorDragDrop stub removed in Session 19; upstream TextEditorDragDrop.cs is now active.

    // TextEditorTables stub removed in Session 17; upstream TextEditorTables.cs is now active.

    // TextRangeEditTables stub removed in Session 24; upstream TextRangeEditTables.cs is now active.

}
#endif
