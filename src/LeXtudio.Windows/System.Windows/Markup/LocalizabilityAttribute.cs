namespace System.Windows.Markup
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class LocalizabilityAttribute : Attribute
    {
        public LocalizabilityAttribute(LocalizationCategory category) { }

        public Modifiability Modifiability { get; set; }
    }
}
