using System.Collections.Generic;
using System.IO;
using System.Text;

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

        internal static string GetTextInternal(ITextPointer startPosition, ITextPointer endPosition)
        {
            if (startPosition is null || endPosition is null)
                return string.Empty;
            var sb = new StringBuilder();
            AppendSegmentText(sb, startPosition, endPosition);
            return sb.ToString();
        }

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
            if (range is null || anchorPosition is null || movingPosition is null)
                return;

            // Order positions so Start <= End. The interface back-door
            // (ITextRange._TextSegments) gives us write access to the range's
            // private segment list; without this, ITextRange.Start/End/IsEmpty
            // trampoline back through TextRangeBase and stack-overflow.
            ITextPointer start, end;
            if (anchorPosition.CompareTo(movingPosition) <= 0)
            {
                start = anchorPosition;
                end = movingPosition;
            }
            else
            {
                start = movingPosition;
                end = anchorPosition;
            }

            range._TextSegments = new List<TextSegment> { new(start, end) };
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

        internal static string GetText(ITextRange range)
        {
            if (range is null)
                return string.Empty;

            var segments = range._TextSegments;
            if (segments is null || segments.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            for (var i = 0; i < segments.Count; i++)
            {
                AppendSegmentText(sb, segments[i].Start, segments[i].End);
            }
            return sb.ToString();
        }

        // Walks the text tree from start (inclusive) to end (exclusive), emitting
        // plain-text content using WPF's pointer semantics: Run characters are
        // appended verbatim, paragraph/list-item ElementEnd emits "\r\n", and
        // embedded UI elements contribute a single space placeholder.
        private static void AppendSegmentText(StringBuilder sb, ITextPointer start, ITextPointer end)
        {
            if (start is null || end is null || start.CompareTo(end) >= 0)
                return;

            var pos = start.CreatePointer();
            while (pos.CompareTo(end) < 0)
            {
                var context = pos.GetPointerContext(LogicalDirection.Forward);
                switch (context)
                {
                    case TextPointerContext.Text:
                    {
                        var runLen = pos.GetTextRunLength(LogicalDirection.Forward);
                        var remaining = pos.GetOffsetToPosition(end);
                        var take = remaining > 0 && remaining < runLen ? remaining : runLen;
                        if (take <= 0)
                            return;

                        var buffer = new char[take];
                        pos.GetTextInRun(LogicalDirection.Forward, buffer, 0, take);
                        sb.Append(buffer, 0, take);

                        if (!pos.MoveToNextContextPosition(LogicalDirection.Forward))
                            return;
                        break;
                    }
                    case TextPointerContext.ElementEnd:
                    {
                        if (pos.GetAdjacentElement(LogicalDirection.Forward) is Paragraph)
                            sb.Append("\r\n");

                        if (!pos.MoveToNextContextPosition(LogicalDirection.Forward))
                            return;
                        break;
                    }
                    case TextPointerContext.EmbeddedElement:
                    {
                        sb.Append(' ');
                        if (!pos.MoveToNextContextPosition(LogicalDirection.Forward))
                            return;
                        break;
                    }
                    default:
                    {
                        if (!pos.MoveToNextContextPosition(LogicalDirection.Forward))
                            return;
                        break;
                    }
                }
            }
        }

        internal static void SetText(ITextRange range, string textData)
        {
        }

        internal static TextSegment GetAutoWord(ITextRange range, ITextPointer position) => new(position, position);

        internal static TextSegment GetAutoWord(ITextRange range, ITextPointer position, LogicalDirection direction) => new(position, position);

        internal static TextSegment GetAutoWord(ITextRange thisRange)
            => new(thisRange.Start, thisRange.End);

        internal static ITextPointer GetStart(ITextRange thisRange)
        {
            var segments = thisRange?._TextSegments;
            return segments is { Count: > 0 } ? segments[0].Start : null!;
        }

        internal static ITextPointer GetEnd(ITextRange thisRange)
        {
            var segments = thisRange?._TextSegments;
            return segments is { Count: > 0 } ? segments[^1].End : null!;
        }

        internal static bool GetIsEmpty(ITextRange thisRange)
        {
            var segments = thisRange?._TextSegments;
            if (segments is null || segments.Count == 0)
                return true;
            var start = segments[0].Start;
            var end = segments[^1].End;
            return start is null || end is null || start.CompareTo(end) == 0;
        }

        internal static List<TextSegment> GetTextSegments(ITextRange thisRange)
            => thisRange?._TextSegments ?? new List<TextSegment>();

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
