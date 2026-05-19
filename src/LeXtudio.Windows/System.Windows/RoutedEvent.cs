namespace System.Windows
{
    public class RoutedEvent
    {
        public RoutedEvent() { }
        public RoutedEvent(string name, Type handlerType) { Name = name; HandlerType = handlerType; }
        public string Name { get; }
        public Type HandlerType { get; }
        public RoutedEvent AddOwner(Type ownerType) => this;
    }
}
