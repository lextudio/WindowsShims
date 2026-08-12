#if WINDOWS_APP_SDK
using System.Runtime.InteropServices;

namespace MS.Internal.Documents;

// Native Win32 spell-checking API (spellcheck.h) used in place of Uno's
// ISpellCheckingService on this TFM: Microsoft.UI.Xaml.Documents resolves to
// the real WinAppSDK assemblies here, which don't define that Uno-only
// extensibility type. Exposes the same SpellCheck(wordBoundaries, text) shape
// so FlowDocumentView.RefreshSpellCheckSquiggles needs no branching.
internal sealed class Win32SpellChecker
{
    private static readonly Guid ClsidSpellCheckerFactory = new("7AB36653-1796-484B-BDFA-E74F1DB7C1DC");

    private readonly ISpellChecker _checker;

    private Win32SpellChecker(ISpellChecker checker) => _checker = checker;

    internal static Win32SpellChecker? TryCreate(string languageTag = "en-US")
    {
        try
        {
            var factoryType = Type.GetTypeFromCLSID(ClsidSpellCheckerFactory);
            if (factoryType is null || Activator.CreateInstance(factoryType) is not ISpellCheckerFactory factory)
                return null;

            var checker = factory.CreateSpellChecker(languageTag);
            return checker is null ? null : new Win32SpellChecker(checker);
        }
        catch
        {
            // No spell-check provider registered for the requested language,
            // or the OS component isn't present; spell-check stays disabled.
            return null;
        }
    }

    // Mirrors Uno's ISpellCheckingService.SpellCheck: one entry per word in
    // wordBoundaries (word i spans [wordBoundaries[i-1], wordBoundaries[i]),
    // or [0, wordBoundaries[0]) for i == 0), holding the misspelled range
    // relative to that word's start, or null when the word is clean.
    internal List<(int, int)?> SpellCheck(List<int> wordBoundaries, string text)
    {
        var result = new List<(int, int)?>(wordBoundaries.Count);
        for (int i = 0; i < wordBoundaries.Count; i++)
            result.Add(null);

        if (wordBoundaries.Count == 0 || string.IsNullOrEmpty(text))
            return result;

        IEnumSpellingError errors;
        try
        {
            errors = _checker.Check(text);
        }
        catch
        {
            return result;
        }
        if (errors is null)
            return result;

        int wordIndex = 0;
        while (errors.Next(out var error) == 0 && error is not null)
        {
            if (error.GetCorrectiveAction() == CorrectiveAction.None)
                continue;

            int errorStart = (int)error.GetStartIndex();
            int errorEnd = errorStart + (int)error.GetLength();
            if (errorEnd <= errorStart)
                continue;

            while (wordIndex < wordBoundaries.Count && wordBoundaries[wordIndex] <= errorStart)
                wordIndex++;
            if (wordIndex >= wordBoundaries.Count)
                break;

            int wordStart = wordIndex == 0 ? 0 : wordBoundaries[wordIndex - 1];
            int wordEnd = wordBoundaries[wordIndex];
            int clampedStart = Math.Max(errorStart, wordStart) - wordStart;
            int clampedEnd = Math.Min(errorEnd, wordEnd) - wordStart;
            if (clampedEnd > clampedStart)
                result[wordIndex] = (clampedStart, clampedEnd);
        }

        return result;
    }

    private enum CorrectiveAction
    {
        None = 0,
        GetSuggestions = 1,
        Replace = 2,
        Delete = 3,
    }

    [ComImport, Guid("B6FD0B71-E2BC-4653-8D05-9FE4DDDE5B76"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ISpellCheckerFactory
    {
        // get_SupportedLanguages — slot 0, unused; kept only to preserve vtable order.
        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetSupportedLanguages_Unused();

        [return: MarshalAs(UnmanagedType.Bool)]
        bool IsSupported([MarshalAs(UnmanagedType.LPWStr)] string languageTag);

        [return: MarshalAs(UnmanagedType.Interface)]
        ISpellChecker CreateSpellChecker([MarshalAs(UnmanagedType.LPWStr)] string languageTag);
    }

    [ComImport, Guid("B6FD0B69-9DDD-4955-A825-260058BEB35F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ISpellChecker
    {
        // get_LanguageTag — slot 0, unused; kept only to preserve vtable order.
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetLanguageTag_Unused();

        [return: MarshalAs(UnmanagedType.Interface)]
        IEnumSpellingError Check([MarshalAs(UnmanagedType.LPWStr)] string text);
    }

    [ComImport, Guid("803E3BD4-2828-4410-8290-418D1D73C762"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IEnumSpellingError
    {
        // Next returns a non-zero (failure) HRESULT once enumeration is
        // exhausted; PreserveSig surfaces that raw code instead of throwing.
        [PreserveSig]
        int Next([MarshalAs(UnmanagedType.Interface)] out ISpellingError? value);
    }

    [ComImport, Guid("B7C82D61-FBE8-4B47-9B27-6C0D2E0DE0A3"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ISpellingError
    {
        [return: MarshalAs(UnmanagedType.U4)]
        uint GetStartIndex();

        [return: MarshalAs(UnmanagedType.U4)]
        uint GetLength();

        CorrectiveAction GetCorrectiveAction();
    }
}
#endif
