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

        static readonly Collections.Generic.Dictionary<ushort, string> _reverse = new()
        {
            [100] = "Thin",
            [200] = "ExtraLight",
            [300] = "Light",
            [400] = "Normal",
            [500] = "Medium",
            [600] = "SemiBold",
            [700] = "Bold",
            [800] = "ExtraBold",
            [900] = "Black",
        };

        public string ConvertToInvariantString(object value)
        {
            // Emit WPF-faithful named weights ("Bold", "Normal", ...), which is what
            // upstream consumers (e.g. XamlToRtfWriter) parse; numeric values like "700"
            // would silently lose the weight in XAML→RTF conversion.
            if (value is FontWeight fw && _reverse.TryGetValue(fw.Weight, out var name))
                return name;
            return value?.ToString() ?? "";
        }
    }
}
