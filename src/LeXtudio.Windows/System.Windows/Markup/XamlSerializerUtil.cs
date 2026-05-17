namespace System.Windows.Markup;

internal static class XamlSerializerUtil
{
    internal static void ThrowIfNonWhiteSpaceInAddText(string s, object parent)
    {
        if (s is null)
        {
            return;
        }

        for (var i = 0; i < s.Length; i++)
        {
            if (!char.IsWhiteSpace(s[i]))
            {
                throw new ArgumentException(SR.Format(SR.NonWhiteSpaceInAddText, s));
            }
        }
    }
}
