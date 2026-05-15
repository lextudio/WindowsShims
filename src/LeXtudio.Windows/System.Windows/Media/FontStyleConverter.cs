namespace System.Windows.Media
{
    /// <summary>Parses font-style strings. Shim for WPF's TypeConverter.</summary>
    public sealed class FontStyleConverter
    {
        public object? ConvertFromInvariantString(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            return value.Trim().ToLowerInvariant() switch
            {
                "normal"  => Windows.FontStyle.Normal,
                "italic"  => Windows.FontStyle.Italic,
                "oblique" => Windows.FontStyle.Oblique,
                _ => throw new FormatException($"Unknown font style: '{value}'")
            };
        }

        public string ConvertToInvariantString(object value)
        {
            if (value is Windows.FontStyle fs) return fs.ToString();
            return value?.ToString() ?? "";
        }
    }
}
