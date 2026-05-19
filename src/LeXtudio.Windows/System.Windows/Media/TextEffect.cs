namespace System.Windows.Media
{
    /// <summary>Portable shim for System.Windows.Media.TextEffect.</summary>
    public class TextEffect
    {
        public int PositionStart { get; set; }
        public int PositionCount { get; set; }
        public TextEffect Clone() => (TextEffect)MemberwiseClone();
    }
}
