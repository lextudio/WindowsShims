namespace System.Windows.Input
{
    public static class NavigationCommands
    {
        public static RoutedUICommand BrowseBack { get; } = new RoutedUICommand("Back", nameof(BrowseBack), typeof(NavigationCommands));
        public static RoutedUICommand BrowseForward { get; } = new RoutedUICommand("Forward", nameof(BrowseForward), typeof(NavigationCommands));
        public static RoutedUICommand Search { get; } = new RoutedUICommand("Search", nameof(Search), typeof(NavigationCommands));
        public static RoutedUICommand Refresh { get; } = new RoutedUICommand("Refresh", nameof(Refresh), typeof(NavigationCommands));
    }
}
