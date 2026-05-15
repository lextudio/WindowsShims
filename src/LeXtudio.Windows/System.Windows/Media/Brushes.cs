namespace System.Windows.Media
{
    public static class Brushes
    {
        // Return SolidColorBrush (Microsoft.UI.Xaml.Media.SolidColorBrush) as Brush
        // SolidColorBrush IS-A Brush since Brush inherits from Microsoft.UI.Xaml.Media.Brush
        public static Brush Black => (Brush)(Microsoft.UI.Xaml.Media.Brush)new SolidColorBrush(Colors.Black);
        public static Brush White => (Brush)(Microsoft.UI.Xaml.Media.Brush)new SolidColorBrush(Colors.White);
        public static Brush Gray => (Brush)(Microsoft.UI.Xaml.Media.Brush)new SolidColorBrush(Colors.Gray);
        public static Brush DarkGray => (Brush)(Microsoft.UI.Xaml.Media.Brush)new SolidColorBrush(Colors.DarkGray);
        public static Brush LightGray => (Brush)(Microsoft.UI.Xaml.Media.Brush)new SolidColorBrush(Colors.LightGray);
        public static Brush Transparent => (Brush)(Microsoft.UI.Xaml.Media.Brush)new SolidColorBrush(Colors.Transparent);
    }
}
