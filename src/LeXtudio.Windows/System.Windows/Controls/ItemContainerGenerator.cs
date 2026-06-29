namespace System.Windows.Controls
{
    // Session 27: the generator holds a registry of the containers the shim
    // render path builds (item <-> container, in display order), so the linked
    // WPF code can resolve rows via ContainerFromItem/Index, IndexFromContainer,
    // and ItemFromContainer. No virtualization/recycling yet — the render path
    // resets and repopulates the registry on every rebuild.
    public sealed class ItemContainerGenerator : Primitives.IRecyclingItemContainerGenerator
    {
        private readonly ItemsControl? _owner;
        private readonly List<DependencyObject> _containers = new();
        private readonly List<object?> _items = new();
        private GeneratorSession? _activeSession;

        public ItemContainerGenerator()
        {
        }

        internal ItemContainerGenerator(ItemsControl owner)
        {
            _owner = owner;
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

        // Virtualized path: a recycled container leaves the realized set. Remove its
        // item<->container entry so ContainerFromItem/IndexFromContainer stay accurate.
        internal void UnregisterContainer(DependencyObject container)
        {
            var index = _containers.IndexOf(container);
            if (index >= 0)
            {
                _containers.RemoveAt(index);
                _items.RemoveAt(index);
            }
        }

        internal void NotifyContainersGenerated()
            => StatusChanged?.Invoke(this, EventArgs.Empty);

        ItemContainerGenerator Primitives.IItemContainerGenerator.GetItemContainerGeneratorForPanel(Panel panel) => this;

        internal void PrepareItemContainer(DependencyObject container)
            => ((Primitives.IItemContainerGenerator)this).PrepareItemContainer(container);

        IDisposable Primitives.IItemContainerGenerator.StartAt(GeneratorPosition position, Primitives.GeneratorDirection direction)
            => ((Primitives.IItemContainerGenerator)this).StartAt(position, direction, false);

        IDisposable Primitives.IItemContainerGenerator.StartAt(GeneratorPosition position, Primitives.GeneratorDirection direction, bool allowStartAtRealizedItem)
        {
            _activeSession?.Dispose();
            _activeSession = new GeneratorSession(this, position, direction, allowStartAtRealizedItem);
            return _activeSession;
        }

        DependencyObject? Primitives.IItemContainerGenerator.GenerateNext()
            => ((Primitives.IItemContainerGenerator)this).GenerateNext(out _);

        DependencyObject? Primitives.IItemContainerGenerator.GenerateNext(out bool isNewlyRealized)
        {
            if (_activeSession is null)
            {
                isNewlyRealized = false;
                return null;
            }

            return _activeSession.GenerateNext(out isNewlyRealized);
        }

        GeneratorPosition Primitives.IItemContainerGenerator.GeneratorPositionFromIndex(int itemIndex)
            => itemIndex < 0 ? new GeneratorPosition(-1, 0) : new GeneratorPosition(itemIndex, 0);

        void Primitives.IItemContainerGenerator.PrepareItemContainer(DependencyObject container)
        {
            var index = IndexFromContainer(container);
            if (index >= 0 && index < _items.Count)
            {
                _owner?.PrepareContainerForItem(container, _items[index]);
            }
        }

        void Primitives.IItemContainerGenerator.Remove(GeneratorPosition position, int count)
        {
            var startIndex = position.Index;
            if (position.Offset > 0)
            {
                startIndex++;
            }

            if (startIndex < 0 || count <= 0 || startIndex >= _containers.Count)
            {
                return;
            }

            var actualCount = Math.Min(count, _containers.Count - startIndex);
            for (var i = 0; i < actualCount; i++)
            {
                var item = _items[startIndex];
                var container = _containers[startIndex];
                _owner?.ClearContainerForItem(container, item);
                _items.RemoveAt(startIndex);
                _containers.RemoveAt(startIndex);
            }
        }

        void Primitives.IRecyclingItemContainerGenerator.Recycle(GeneratorPosition position, int count)
        {
            var startIndex = position.Index;
            if (position.Offset > 0)
            {
                startIndex++;
            }

            if (startIndex < 0 || count <= 0 || startIndex >= _containers.Count)
            {
                return;
            }

            var actualCount = Math.Min(count, _containers.Count - startIndex);
            for (var i = 0; i < actualCount; i++)
            {
                RecyclableContainers.Enqueue(_containers[startIndex + i]);
            }

            ((Primitives.IItemContainerGenerator)this).Remove(position, count);
        }

        private sealed class GeneratorSession : IDisposable
        {
            private readonly ItemContainerGenerator _ownerGenerator;
            private readonly Primitives.GeneratorDirection _direction;
            private int _index;

            public GeneratorSession(
                ItemContainerGenerator ownerGenerator,
                GeneratorPosition position,
                Primitives.GeneratorDirection direction,
                bool allowStartAtRealizedItem)
            {
                _ownerGenerator = ownerGenerator;
                _direction = direction;

                var index = position.Index;
                if (position.Offset != 0)
                {
                    index += position.Offset;
                }
                else if (!allowStartAtRealizedItem)
                {
                    index += direction == Primitives.GeneratorDirection.Forward ? 1 : -1;
                }

                _index = index;
            }

            public DependencyObject? GenerateNext(out bool isNewlyRealized)
            {
                if (_direction != Primitives.GeneratorDirection.Forward)
                {
                    isNewlyRealized = false;
                    return null;
                }

                if (_ownerGenerator._owner is null || _index < 0 || _index >= _ownerGenerator._owner.Items.Count)
                {
                    isNewlyRealized = false;
                    return null;
                }

                if (_index < _ownerGenerator._containers.Count)
                {
                    var realized = _ownerGenerator._containers[_index];
                    isNewlyRealized = false;
                    _index++;
                    return realized;
                }

                var item = _ownerGenerator._owner.Items[_index];
                DependencyObject? container = null;
                if (_ownerGenerator.RecyclableContainers.Count > 0)
                {
                    container = _ownerGenerator.RecyclableContainers.Dequeue();
                }

                container ??= _ownerGenerator._owner.CreateContainerForItem(item);
                if (container is null)
                {
                    isNewlyRealized = false;
                    return null;
                }

                if (_index == _ownerGenerator._containers.Count)
                {
                    _ownerGenerator._items.Add(item);
                    _ownerGenerator._containers.Add(container);
                }
                else
                {
                    _ownerGenerator._items.Insert(_index, item);
                    _ownerGenerator._containers.Insert(_index, container);
                }

                _ownerGenerator._owner.PrepareContainerForItem(container, item);
                isNewlyRealized = true;
                _index++;
                return container;
            }

            public void Dispose()
            {
                if (ReferenceEquals(_ownerGenerator._activeSession, this))
                {
                    _ownerGenerator._activeSession = null;
                }
            }
        }
    }
}

namespace System.Windows.Controls.Primitives
{
    public interface IItemContainerGenerator
    {
        Controls.ItemContainerGenerator GetItemContainerGeneratorForPanel(Controls.Panel panel);

        IDisposable StartAt(GeneratorPosition position, GeneratorDirection direction);

        IDisposable StartAt(GeneratorPosition position, GeneratorDirection direction, bool allowStartAtRealizedItem);

        DependencyObject? GenerateNext();

        DependencyObject? GenerateNext(out bool isNewlyRealized);

        GeneratorPosition GeneratorPositionFromIndex(int itemIndex);

        void PrepareItemContainer(DependencyObject container);

        void Remove(GeneratorPosition position, int count);
    }

    public interface IRecyclingItemContainerGenerator : IItemContainerGenerator
    {
        void Recycle(GeneratorPosition position, int count);
    }

    public enum GeneratorDirection
    {
        Forward = 0,
        Backward = 1,
    }

    public enum GeneratorStatus
    {
        NotStarted,
        GeneratingContainers,
        ContainersGenerated,
        Error,
    }
}
