namespace System.ComponentModel
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Event | AttributeTargets.Field)]
    public class CustomCategoryAttribute : Attribute
    {
        public CustomCategoryAttribute(string category) { }
    }
}
