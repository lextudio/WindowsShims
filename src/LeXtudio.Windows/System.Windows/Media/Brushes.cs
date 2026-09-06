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
        public static Brush LightGreen => (Brush)(Microsoft.UI.Xaml.Media.Brush)new SolidColorBrush(Colors.LightGreen);
        public static Brush LightPink => (Brush)(Microsoft.UI.Xaml.Media.Brush)new SolidColorBrush(Colors.LightPink);
        public static Brush LightBlue => (Brush)(Microsoft.UI.Xaml.Media.Brush)new SolidColorBrush(Colors.LightBlue);
        public static Brush Red          => (Brush)(Microsoft.UI.Xaml.Media.Brush)new SolidColorBrush(Colors.Red);
        public static Brush Green        => (Brush)(Microsoft.UI.Xaml.Media.Brush)new SolidColorBrush(Colors.Green);
        public static Brush Blue         => (Brush)(Microsoft.UI.Xaml.Media.Brush)new SolidColorBrush(Colors.Blue);
        public static Brush Yellow       => (Brush)(Microsoft.UI.Xaml.Media.Brush)new SolidColorBrush(Colors.Yellow);
        public static Brush Maroon       => (Brush)(Microsoft.UI.Xaml.Media.Brush)new SolidColorBrush(Colors.Maroon);
        public static Brush CadetBlue    => (Brush)(Microsoft.UI.Xaml.Media.Brush)new SolidColorBrush(Colors.CadetBlue);
        public static Brush DarkViolet   => (Brush)(Microsoft.UI.Xaml.Media.Brush)new SolidColorBrush(Colors.DarkViolet);
        public static Brush DarkMagenta  => (Brush)(Microsoft.UI.Xaml.Media.Brush)new SolidColorBrush(Colors.DarkMagenta);
        public static Brush DarkGreen    => (Brush)(Microsoft.UI.Xaml.Media.Brush)new SolidColorBrush(Colors.DarkGreen);
        public static Brush DarkBlue     => (Brush)(Microsoft.UI.Xaml.Media.Brush)new SolidColorBrush(Colors.DarkBlue);
    }
}
