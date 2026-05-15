namespace System.Windows
{
    public static class EventManager
    {
        public static void RegisterClassHandler(Type classType, RoutedEvent routedEvent, Delegate handler) { }
        public static RoutedEvent RegisterRoutedEvent(string name, RoutingStrategy routingStrategy, Type handlerType, Type ownerType) => new();
    }
}
