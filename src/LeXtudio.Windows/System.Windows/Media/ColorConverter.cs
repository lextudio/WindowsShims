namespace System.Windows.Media
{
    /// <summary>
    /// Parses CSS-style color strings (#RGB, #RRGGBB, #AARRGGBB) and a subset of named colors.
    /// Used by the Xshd XSLT loader as a shim for WPF's ColorConverter.
    /// </summary>
    public sealed class ColorConverter
    {
        static readonly Collections.Generic.Dictionary<string, Color> _named =
            new Collections.Generic.Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase)
        {
            ["Black"]        = Colors.Black,       ["White"]        = Colors.White,
            ["Red"]          = Colors.Red,         ["Green"]        = Colors.Green,
            ["Blue"]         = Colors.Blue,        ["Yellow"]       = Colors.Yellow,
            ["Cyan"]         = Colors.Cyan,        ["Magenta"]      = Colors.Magenta,
            ["Gray"]         = Colors.Gray,        ["Grey"]         = Colors.Gray,
            ["Silver"]       = Colors.Silver,      ["DarkGray"]     = Colors.DarkGray,
            ["LightGray"]    = Colors.LightGray,   ["Orange"]       = Colors.Orange,
            ["DarkBlue"]     = Colors.DarkBlue,    ["DarkRed"]      = Colors.DarkRed,
            ["DarkGreen"]    = Colors.DarkGreen,   ["Navy"]         = Colors.Navy,
            ["Teal"]         = Colors.Teal,        ["Purple"]       = Colors.Purple,
            ["Brown"]        = Colors.Brown,       ["Pink"]         = Colors.Pink,
            ["Transparent"]  = Colors.Transparent,
            ["MidnightBlue"] = Colors.MidnightBlue, ["DarkCyan"]    = Colors.DarkCyan,
            ["DarkMagenta"]  = Colors.DarkMagenta,  ["DarkSlateGray"]= Colors.DarkSlateGray,
            ["DeepPink"]     = Colors.DeepPink,     ["Fuchsia"]     = Colors.Fuchsia,
            ["Maroon"]       = Colors.Maroon,       ["Olive"]       = Colors.Olive,
            ["SaddleBrown"]  = Colors.SaddleBrown,  ["Sienna"]      = Colors.Sienna,
            ["SlateGray"]    = Colors.SlateGray,
        };

        public static object? ConvertFromString(string value) => new ColorConverter().ConvertFromInvariantString(value);

        public object? ConvertFromInvariantString(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            string v = value.Trim();
            if (v.StartsWith("#", StringComparison.Ordinal))
            {
                string hex = v.Substring(1);
                if (hex.Length == 3)
                {
                    byte r = Convert.ToByte(new string(hex[0], 2), 16);
                    byte g = Convert.ToByte(new string(hex[1], 2), 16);
                    byte b = Convert.ToByte(new string(hex[2], 2), 16);
                    return Color.FromArgb(255, r, g, b);
                }
                if (hex.Length == 6)
                {
                    byte r = Convert.ToByte(hex.Substring(0, 2), 16);
                    byte g = Convert.ToByte(hex.Substring(2, 2), 16);
                    byte b = Convert.ToByte(hex.Substring(4, 2), 16);
                    return Color.FromArgb(255, r, g, b);
                }
                if (hex.Length == 8)
                {
                    byte a = Convert.ToByte(hex.Substring(0, 2), 16);
                    byte r = Convert.ToByte(hex.Substring(2, 2), 16);
                    byte g = Convert.ToByte(hex.Substring(4, 2), 16);
                    byte b = Convert.ToByte(hex.Substring(6, 2), 16);
                    return Color.FromArgb(a, r, g, b);
                }
                throw new FormatException($"Unrecognized color format: '{value}'");
            }
            if (_named.TryGetValue(v, out Color named)) return named;
            throw new FormatException($"Unknown color name: '{value}'");
        }
    }
}
