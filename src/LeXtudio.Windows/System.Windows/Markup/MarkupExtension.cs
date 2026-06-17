// Compiler shims for the WPF markup-extension surface used by linked WPF sources
// (e.g. ResourceKey / ComponentResourceKey).
//
// Provided ONLY on the WinUI (WINDOWS_APP_SDK) target. On the Uno desktop target the equivalent
// System.Windows.Markup.MarkupExtension comes from Uno.Xaml, and shipping a second copy here makes
// the reference ambiguous (CS0433) — which is why the earlier "Clean up ILSpy dependencies" commit
// removed the unconditional shim. The WinUI target has no Uno.Xaml, so it needs these.
#if WINDOWS_APP_SDK
using System;

namespace System.Windows.Markup
{
    /// <summary>Compiler shim for the WPF markup extension base class.</summary>
    public abstract class MarkupExtension
    {
        protected MarkupExtension() { }

        public abstract object ProvideValue(IServiceProvider serviceProvider);
    }

    /// <summary>Compiler shim for WPF's MarkupExtensionReturnTypeAttribute (applied to ResourceKey).</summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class MarkupExtensionReturnTypeAttribute : Attribute
    {
        public MarkupExtensionReturnTypeAttribute() { }

        public MarkupExtensionReturnTypeAttribute(Type returnType)
        {
            ReturnType = returnType;
        }

        public MarkupExtensionReturnTypeAttribute(Type returnType, Type expressionType)
        {
            ReturnType = returnType;
            ExpressionType = expressionType;
        }

        public Type ReturnType { get; }

        public Type ExpressionType { get; }
    }
}
#endif
