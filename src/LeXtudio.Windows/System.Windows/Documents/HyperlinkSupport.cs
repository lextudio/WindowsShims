using System.Collections.Generic;
using System.IO;

namespace System.Windows.Documents
{
    public sealed partial class FixedPage : Microsoft.UI.Xaml.DependencyObject
    {
        public static readonly Microsoft.UI.Xaml.DependencyProperty NavigateUriProperty =
            Microsoft.UI.Xaml.DependencyProperty.RegisterAttached(
                "NavigateUri",
                typeof(System.Uri),
                typeof(FixedPage),
                new System.Windows.FrameworkPropertyMetadata(null));

        public static System.Uri GetLinkUri(System.Windows.Input.IInputElement sourceElement, System.Uri targetUri) => targetUri;
    }

    public static class BaseUriHelper
    {
        public static readonly Microsoft.UI.Xaml.DependencyProperty BaseUriProperty =
            Microsoft.UI.Xaml.DependencyProperty.Register(
                "BaseUri",
                typeof(System.Uri),
                typeof(BaseUriHelper),
                new System.Windows.FrameworkPropertyMetadata(null));
    }

    public static class TextRangeBase
    {
        internal static bool Contains(ITextRange thisRange, ITextPointer textPointer)
        {
            if (thisRange is null || textPointer is null || thisRange.IsEmpty)
            {
                return false;
            }

            var startCompare = textPointer.CompareTo(thisRange.Start);
            var endCompare = textPointer.CompareTo(thisRange.End);
            return startCompare >= 0 && endCompare <= 0;
        }

        public static string GetTextInternal(object start, object end) => string.Empty;

        internal static string GetTextInternal(ITextPointer startPosition, ITextPointer endPosition) => string.Empty;

        internal static void BeginChange(ITextRange range)
        {
        }

        internal static void BeginChangeNoUndo(ITextRange thisRange)
        {
            BeginChange(thisRange);
        }

        internal static void EndChange(ITextRange range)
        {
        }

        internal static void EndChange(ITextRange thisRange, bool disableScroll, bool skipEvents)
        {
            EndChange(thisRange);
        }

        internal static void Select(ITextRange range, ITextPointer anchorPosition, ITextPointer movingPosition)
        {
        }

        internal static void Select(ITextRange thisRange, ITextPointer position1, ITextPointer position2, bool includeCellAtMovingPosition)
        {
            Select(thisRange, position1, position2);
        }

        internal static void SelectWord(ITextRange range, ITextPointer position)
        {
        }

        internal static void SelectParagraph(ITextRange range, ITextPointer position)
        {
        }

        internal static void ApplyInitialTypingHeuristics(ITextRange range, bool overType)
        {
        }

        internal static void ApplyInitialTypingHeuristics(ITextRange thisRange)
        {
            ApplyInitialTypingHeuristics(thisRange, false);
        }

        internal static void ApplyFinalTypingHeuristics(ITextRange range)
        {
        }

        internal static void ApplyFinalTypingHeuristics(ITextRange thisRange, bool overType)
        {
            ApplyFinalTypingHeuristics(thisRange);
        }

        internal static void ApplyTypingHeuristics(ITextRange thisRange, bool overType)
        {
            ApplyInitialTypingHeuristics(thisRange);
            ApplyFinalTypingHeuristics(thisRange, overType);
        }

        internal static object? GetPropertyValue(ITextRange range, DependencyProperty formattingProperty) => DependencyProperty.UnsetValue;

        internal static bool IsParagraphBoundaryCrossed(ITextRange thisRange) => false;

        internal static void NotifyChanged(ITextRange thisRange, bool disableScroll)
        {
            NotifyChanged(thisRange, disableScroll, false);
        }

        internal static void NotifyChanged(ITextRange range, bool disableScroll, bool skipEvents)
        {
        }

        internal static string GetText(ITextRange range) => string.Empty;

        internal static void SetText(ITextRange range, string textData)
        {
        }

        internal static TextSegment GetAutoWord(ITextRange range, ITextPointer position) => new(position, position);

        internal static TextSegment GetAutoWord(ITextRange range, ITextPointer position, LogicalDirection direction) => new(position, position);

        internal static TextSegment GetAutoWord(ITextRange thisRange)
            => new(thisRange.Start, thisRange.End);

        internal static ITextPointer GetStart(ITextRange thisRange) => thisRange.Start;

        internal static ITextPointer GetEnd(ITextRange thisRange) => thisRange.End;

        internal static bool GetIsEmpty(ITextRange thisRange) => thisRange.IsEmpty;

        internal static List<TextSegment> GetTextSegments(ITextRange thisRange)
            => [new(thisRange.Start, thisRange.End)];

        internal static string GetXml(ITextRange thisRange) => string.Empty;

        internal static bool CanSave(ITextRange thisRange, string dataFormat) => true;

        internal static bool CanLoad(ITextRange thisRange, string dataFormat) => true;

        internal static void Save(ITextRange thisRange, Stream stream, string dataFormat, bool preserveTextElements)
        {
        }

        internal static void Load(TextRange thisRange, Stream stream, string dataFormat)
        {
        }

        internal static int GetChangeBlockLevel(ITextRange thisRange) => 0;

        internal static UIElement? GetUIElementSelected(ITextRange range) => null;

        internal static bool GetIsTableCellRange(ITextRange thisRange) => false;
    }

    public sealed class RequestSetStatusBarEventArgs : System.Windows.RoutedEventArgs
    {
        public RequestSetStatusBarEventArgs(System.Uri? uri)
        {
            Uri = uri;
        }

        public static RequestSetStatusBarEventArgs Clear { get; } = new(null);
        public System.Uri? Uri { get; }
    }
}
