namespace System.Windows
{
    /// <summary>
    /// WPF's per-type token in the property system. Uno has no equivalent registry, so this
    /// carries the CLR type and answers the one relationship question callers ask.
    /// </summary>
    public class DependencyObjectType
    {
        private DependencyObjectType(Type? systemType) => SystemType = systemType;

        public Type? SystemType { get; }

        public string Name => SystemType?.Name ?? string.Empty;

        public static DependencyObjectType FromSystemTypeInternal(Type systemType) => new(systemType);

        public static DependencyObjectType FromSystemType(Type systemType) => new(systemType);

        public bool IsSubclassOf(DependencyObjectType? other)
            => SystemType is not null
                && other?.SystemType is not null
                && SystemType.IsSubclassOf(other.SystemType);
    }
}
