namespace System.Windows.Controls
{
    // Session 27: the generator holds a registry of the containers the shim
    // render path builds (item <-> container, in display order), so the linked
    // WPF code can resolve rows via ContainerFromItem/Index, IndexFromContainer,
    // and ItemFromContainer. No virtualization/recycling yet — the render path
    // resets and repopulates the registry on every rebuild.
    public sealed class ItemContainerGenerator
    {
        private readonly List<DependencyObject> _containers = new();
        private readonly List<object?> _items = new();

        internal ItemContainerGenerator()
        {
        }

        public Primitives.GeneratorStatus Status
            => _containers.Count > 0
                ? Primitives.GeneratorStatus.ContainersGenerated
                : Primitives.GeneratorStatus.NotStarted;

        public event EventHandler? StatusChanged;

        public DependencyObject? ContainerFromIndex(int index)
            => index >= 0 && index < _containers.Count ? _containers[index] : null;

        public DependencyObject? ContainerFromItem(object? item)
        {
            var index = _items.FindIndex(x => ItemsControl.EqualsEx(x, item));
            return index >= 0 ? _containers[index] : null;
        }

        public int IndexFromContainer(DependencyObject container)
            => _containers.IndexOf(container);

        public object? ItemFromContainer(DependencyObject container)
        {
            var index = _containers.IndexOf(container);
            return index >= 0 ? _items[index] : DependencyProperty.UnsetValue;
        }

        internal Queue<DependencyObject> RecyclableContainers { get; } = new();

        // Read-only view of registered containers (display order) for shim
        // selection / iteration.
        internal IReadOnlyList<DependencyObject> Containers => _containers;

        // Render-path registry management.
        internal void ResetContainers()
        {
            _containers.Clear();
            _items.Clear();
        }

        internal void RegisterContainer(object? item, DependencyObject container)
        {
            _items.Add(item);
            _containers.Add(container);
        }

        internal void NotifyContainersGenerated()
            => StatusChanged?.Invoke(this, EventArgs.Empty);
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
