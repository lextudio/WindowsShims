namespace System.Windows
{
    public interface IWeakEventListener
    {
        bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e);
    }

    /// <summary>Portable shim for System.Windows.FontStyles.</summary>
    public static class FontStyles
    {
        public static FontStyle Normal => global::Windows.UI.Text.FontStyle.Normal;
        public static FontStyle Italic => global::Windows.UI.Text.FontStyle.Italic;
        public static FontStyle Oblique => global::Windows.UI.Text.FontStyle.Oblique;
    }

    /// <summary>Portable shim for System.Windows.FontWeights (WPF static class).</summary>
    public static class FontWeights
    {
        public static FontWeight Thin       => new() { Weight = 100 };
        public static FontWeight ExtraLight => new() { Weight = 200 };
        public static FontWeight Light      => new() { Weight = 300 };
        public static FontWeight Normal     => new() { Weight = 400 };
        public static FontWeight Medium     => new() { Weight = 500 };
        public static FontWeight SemiBold   => new() { Weight = 600 };
        public static FontWeight Bold       => new() { Weight = 700 };
        public static FontWeight ExtraBold  => new() { Weight = 800 };
        public static FontWeight Black      => new() { Weight = 900 };
    }

    public enum BaselineAlignment
    {
        Top, Center, Baseline, Bottom, TextTop, TextBottom, Subscript, Superscript
    }

    public readonly struct Vector : IEquatable<Vector>
    {
        public double X { get; }
        public double Y { get; }

        public Vector(double x, double y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(Vector other) => X.Equals(other.X) && Y.Equals(other.Y);
        public override bool Equals(object? obj) => obj is Vector other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Y);
        public static bool operator ==(Vector left, Vector right) => left.Equals(right);
        public static bool operator !=(Vector left, Vector right) => !left.Equals(right);
    }

    public interface IDataObject
    {
        object GetData(string format);
        object GetData(Type format);
        object GetData(string format, bool autoConvert);
        bool GetDataPresent(string format);
        bool GetDataPresent(Type format);
        bool GetDataPresent(string format, bool autoConvert);
        string[] GetFormats();
        string[] GetFormats(bool autoConvert);
        void SetData(object data);
        void SetData(string format, object data);
        void SetData(Type format, object data);
        void SetData(string format, object data, bool autoConvert);
    }

    /// <summary>Compiler shim for DataObject clipboard transfer object.</summary>
    public class DataObject : IDataObject
    {
        readonly Collections.Generic.Dictionary<string, object> _data =
            new Collections.Generic.Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        public DataObject() { }
        public DataObject(string format, object data) { _data[format] = data; }

        public void SetData(string format, object data) => _data[format] = data;
        public void SetData(string format, object data, bool autoConvert) => _data[format] = data;
        public void SetData(object data) => _data[data?.GetType().FullName ?? ""] = data;
        public void SetData(Type format, object data) => _data[format?.FullName ?? ""] = data;
        public void SetImage(Media.Imaging.BitmapSource image) => _data[DataFormats.Bitmap] = image;
        public object GetData(string format) =>
            _data.TryGetValue(format, out object v) ? v : null;
        public object GetData(Type format) =>
            _data.TryGetValue(format?.FullName ?? "", out object v) ? v : null;
        public object GetData(string format, bool autoConvert) => GetData(format);
        public bool GetDataPresent(string format) => _data.ContainsKey(format);
        public bool GetDataPresent(Type format) => _data.ContainsKey(format?.FullName ?? "");
        public bool GetDataPresent(string format, bool autoConvert) => _data.ContainsKey(format);
        public bool ContainsImage() => _data.ContainsKey(DataFormats.Bitmap);
        public string[] GetFormats() => [.. _data.Keys];
        public string[] GetFormats(bool autoConvert) => GetFormats();
    }

    /// <summary>Compiler shim for DataFormats clipboard format constants.</summary>
    public static class DataFormats
    {
        public const string Text         = "Text";
        public const string UnicodeText  = "UnicodeText";
        public const string OemText      = "OemText";
        public const string Rtf          = "Rtf";
        public const string Html         = "Html";
        public const string Xaml         = "Xaml";
        public const string XamlPackage  = "XamlPackage";
        public const string Bitmap       = "Bitmap";
        public const string FileDrop     = "FileDrop";
    }

    /// <summary>Compiler shim wrapping Uno cross-platform clipboard.</summary>
    public static class Clipboard
    {
        public static void SetDataObject(IDataObject data, bool copy) { }
        public static IDataObject GetDataObject() => new DataObject();
        public static void SetText(string text) { }
        public static string GetText() => string.Empty;
    }

    public class DataObjectCopyingEventArgs : RoutedEventArgs
    {
        public DataObjectCopyingEventArgs(IDataObject dataObject, bool isDragDrop)
        {
            DataObject = dataObject;
            IsDragDrop = isDragDrop;
        }

        public IDataObject DataObject { get; }
        public bool IsDragDrop { get; }
        public bool CommandCancelled { get; private set; }
        public void CancelCommand() => CommandCancelled = true;
    }

    public class DataObjectPastingEventArgs : RoutedEventArgs
    {
        public DataObjectPastingEventArgs(IDataObject dataObject, bool isDragDrop, string formatToApply)
        {
            DataObject = dataObject;
            IsDragDrop = isDragDrop;
            FormatToApply = formatToApply;
        }

        public IDataObject DataObject { get; set; }
        public bool IsDragDrop { get; }
        public string FormatToApply { get; set; }
        public bool CommandCancelled { get; private set; }
        public void CancelCommand() => CommandCancelled = true;
    }

    public class DataObjectSettingDataEventArgs : RoutedEventArgs
    {
        public DataObjectSettingDataEventArgs(IDataObject dataObject, string format)
        {
            DataObject = dataObject;
            Format = format;
        }

        public IDataObject DataObject { get; }
        public string Format { get; }
        public bool CommandCancelled { get; private set; }
        public void CancelCommand() => CommandCancelled = true;
    }

    public sealed class SourceChangedEventArgs : EventArgs
    {
        public PresentationSource NewSource { get; init; }
        public PresentationSource OldSource { get; init; }
    }

    public delegate void SourceChangedEventHandler(object sender, SourceChangedEventArgs args);

    public class PresentationSource
    {
        public Media.CompositionTarget CompositionTarget { get; set; }

        public static PresentationSource FromVisual(Visual visual) => null;
        public static PresentationSource CriticalFromVisual(object visual) => null;
        public static void AddSourceChangedHandler(object element, SourceChangedEventHandler handler) { }
        public static void RemoveSourceChangedHandler(object element, SourceChangedEventHandler handler) { }
    }

    /// <summary>Portable shim for System.Windows.FontStretches.</summary>
    public static class FontStretches
    {
        public static FontStretch UltraCondensed => global::Windows.UI.Text.FontStretch.UltraCondensed;
        public static FontStretch ExtraCondensed => global::Windows.UI.Text.FontStretch.ExtraCondensed;
        public static FontStretch Condensed      => global::Windows.UI.Text.FontStretch.Condensed;
        public static FontStretch SemiCondensed  => global::Windows.UI.Text.FontStretch.SemiCondensed;
        public static FontStretch Normal         => global::Windows.UI.Text.FontStretch.Normal;
        public static FontStretch Medium         => global::Windows.UI.Text.FontStretch.Normal;
        public static FontStretch SemiExpanded   => global::Windows.UI.Text.FontStretch.SemiExpanded;
        public static FontStretch Expanded       => global::Windows.UI.Text.FontStretch.Expanded;
        public static FontStretch ExtraExpanded  => global::Windows.UI.Text.FontStretch.ExtraExpanded;
        public static FontStretch UltraExpanded  => global::Windows.UI.Text.FontStretch.UltraExpanded;
    }
}

namespace System.Windows
{
    public static class SystemColors
    {
        public static Brush ControlTextBrush => System.Windows.Media.Brushes.Black;
        public static Brush WindowTextBrush => System.Windows.Media.Brushes.Black;
        public static Brush HighlightTextBrush => System.Windows.Media.Brushes.White;
        public static Brush GrayTextBrush => System.Windows.Media.Brushes.Gray;
        public static Brush ControlBrush => System.Windows.Media.Brushes.LightGray;
        public static Brush WindowBrush => System.Windows.Media.Brushes.White;
        public static Brush HighlightBrush => new SolidColorBrush(System.Windows.Media.Colors.Blue);
        public static Color WindowColor => System.Windows.Media.Colors.White;
        public static Color HighlightColor => System.Windows.Media.Colors.Blue;
    }

    public static class FocusManager
    {
        public static DependencyObject GetFocusScope(DependencyObject element) => element;
    }

    public sealed class InputLanguageEventArgs : EventArgs
    {
        public global::System.Globalization.CultureInfo NewLanguage { get; init; }
            = global::System.Globalization.CultureInfo.CurrentCulture;
        public global::System.Globalization.CultureInfo PreviousLanguage { get; init; }
            = global::System.Globalization.CultureInfo.CurrentCulture;
    }

    public delegate void InputLanguageEventHandler(object sender, InputLanguageEventArgs e);

    public sealed class InputLanguageManager
    {
        public static InputLanguageManager Current { get; } = new();

        public global::System.Globalization.CultureInfo CurrentInputLanguage { get; set; }
            = global::System.Globalization.CultureInfo.CurrentCulture;

        public event InputLanguageEventHandler InputLanguageChanged;
    }
}
