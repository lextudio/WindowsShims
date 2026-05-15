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
                "normal"  => FontStyle.Normal,
                "italic"  => FontStyle.Italic,
                "oblique" => FontStyle.Oblique,
                _ => throw new FormatException($"Unknown font style: '{value}'")
            };
        }

        public string ConvertToInvariantString(object value)
        {
            if (value is FontStyle fs) return fs.ToString();
            return value?.ToString() ?? "";
        }
    }
}
