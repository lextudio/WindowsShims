namespace System.Windows.Controls
{
    // Stub generator: no containers are ever generated, so lookups return
    // null/-1 and the status stays NotStarted. Real generation arrives with
    // the row/container milestone.
    public sealed class ItemContainerGenerator
    {
        internal ItemContainerGenerator()
        {
        }

        public Primitives.GeneratorStatus Status => Primitives.GeneratorStatus.NotStarted;

        public event EventHandler? StatusChanged
        {
            add { }
            remove { }
        }

        public DependencyObject? ContainerFromIndex(int index) => null;

        public DependencyObject? ContainerFromItem(object? item) => null;

        public int IndexFromContainer(DependencyObject container) => -1;

        public object? ItemFromContainer(DependencyObject container) => DependencyProperty.UnsetValue;
    }
}

namespace System.Windows.Controls.Primitives
{
    public enum GeneratorStatus
    {
        NotStarted,
        GeneratingContainers,
        ContainersGenerated,
        Error,
    }
}
