namespace System.Windows.Media
{
    /// <summary>Parses font-weight strings. Shim for WPF's TypeConverter.</summary>
    public sealed class FontWeightConverter
    {
        static readonly Collections.Generic.Dictionary<string, Windows.FontWeight> _map =
            new Collections.Generic.Dictionary<string, Windows.FontWeight>(StringComparer.OrdinalIgnoreCase)
        {
            ["Thin"]       = Windows.FontWeight.FromOpenTypeWeight(100),
            ["ExtraLight"] = Windows.FontWeight.FromOpenTypeWeight(200),
            ["UltraLight"] = Windows.FontWeight.FromOpenTypeWeight(200),
            ["Light"]      = Windows.FontWeight.FromOpenTypeWeight(300),
            ["Normal"]     = Windows.FontWeight.FromOpenTypeWeight(400),
            ["Regular"]    = Windows.FontWeight.FromOpenTypeWeight(400),
            ["Medium"]     = Windows.FontWeight.FromOpenTypeWeight(500),
            ["DemiBold"]   = Windows.FontWeight.FromOpenTypeWeight(600),
            ["SemiBold"]   = Windows.FontWeight.FromOpenTypeWeight(600),
            ["Bold"]       = Windows.FontWeight.FromOpenTypeWeight(700),
            ["ExtraBold"]  = Windows.FontWeight.FromOpenTypeWeight(800),
            ["UltraBold"]  = Windows.FontWeight.FromOpenTypeWeight(800),
            ["Black"]      = Windows.FontWeight.FromOpenTypeWeight(900),
            ["Heavy"]      = Windows.FontWeight.FromOpenTypeWeight(900),
        };

        public object? ConvertFromInvariantString(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            if (_map.TryGetValue(value.Trim(), out Windows.FontWeight fw)) return fw;
            if (int.TryParse(value.Trim(), out int weight))
                return Windows.FontWeight.FromOpenTypeWeight(weight);
            throw new FormatException($"Unknown font weight: '{value}'");
        }

        public string ConvertToInvariantString(object value)
        {
            if (value is Windows.FontWeight fw) return fw.ToString();
            return value?.ToString() ?? "";
        }
    }
}
