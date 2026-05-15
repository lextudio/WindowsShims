using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Controls;

namespace System.Windows.Controls;

public static class TextBlockExtensions
{
    private sealed class TextBlockState
    {
        public bool HasComplexContent { get; set; } = true;
    }

    private static readonly ConditionalWeakTable<TextBlock, TextBlockState> States = new();

    extension(TextBlock textBlock)
    {
        public bool HasComplexContent
        {
            get => States.GetOrCreateValue(textBlock).HasComplexContent;
            set => States.GetOrCreateValue(textBlock).HasComplexContent = value;
        }
    }
}
