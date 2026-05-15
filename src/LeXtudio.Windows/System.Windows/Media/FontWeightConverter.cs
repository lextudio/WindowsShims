namespace System.Windows.Media
{
    /// <summary>Parses font-weight strings. Shim for WPF's TypeConverter.</summary>
    public sealed class FontWeightConverter
    {
        static readonly Collections.Generic.Dictionary<string, FontWeight> _map =
            new Collections.Generic.Dictionary<string, FontWeight>(StringComparer.OrdinalIgnoreCase)
        {
            ["Thin"]       = System.Windows.FontWeights.Thin,
            ["ExtraLight"] = System.Windows.FontWeights.ExtraLight,
            ["UltraLight"] = System.Windows.FontWeights.ExtraLight,
            ["Light"]      = System.Windows.FontWeights.Light,
            ["Normal"]     = System.Windows.FontWeights.Normal,
            ["Regular"]    = System.Windows.FontWeights.Normal,
            ["Medium"]     = System.Windows.FontWeights.Medium,
            ["DemiBold"]   = System.Windows.FontWeights.SemiBold,
            ["SemiBold"]   = System.Windows.FontWeights.SemiBold,
            ["Bold"]       = System.Windows.FontWeights.Bold,
            ["ExtraBold"]  = System.Windows.FontWeights.ExtraBold,
            ["UltraBold"]  = System.Windows.FontWeights.ExtraBold,
            ["Black"]      = System.Windows.FontWeights.Black,
            ["Heavy"]      = System.Windows.FontWeights.Black,
        };

        public object? ConvertFromInvariantString(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            if (_map.TryGetValue(value.Trim(), out FontWeight fw)) return fw;
            if (int.TryParse(value.Trim(), out int weight))
                return new FontWeight { Weight = (ushort)weight };
            throw new FormatException($"Unknown font weight: '{value}'");
        }

        public string ConvertToInvariantString(object value)
        {
            if (value is FontWeight fw) return fw.Weight.ToString();
            return value?.ToString() ?? "";
        }
    }
}
