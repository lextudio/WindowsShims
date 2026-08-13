using Xunit;

namespace LeXtudio.Windows.Tests;

// Covers the native Win32 spell-checking wrapper that backs FlowDocumentView's
// squiggles on WINDOWS_APP_SDK, where Uno's ISpellCheckingService is absent.
// The desktop target keeps using the Hunspell add-in, so these only run under
// the WinAppSDK build.
public sealed class Win32SpellCheckerTests
{
#if WINDOWS_APP_SDK
    // Mirrors FlowDocumentView.GetWords: boundaries are the *end* offset of each
    // word, so word i spans [boundary[i-1], boundary[i]) (0 for the first).
    static List<int> Boundaries(string text)
    {
        var ends = new List<int>();
        int i = 0;
        while (i < text.Length)
        {
            if (char.IsWhiteSpace(text[i])) { i++; continue; }
            while (i < text.Length && !char.IsWhiteSpace(text[i])) i++;
            ends.Add(i);
        }
        return ends;
    }

    // Skips (rather than silently passing) when the OS has no en-US provider, so a
    // run that never exercised the COM path is visible in the results instead of
    // masquerading as coverage.
    static MS.Internal.Documents.Win32SpellChecker RequireChecker()
    {
        var checker = MS.Internal.Documents.Win32SpellChecker.TryCreate();
        if (checker is null)
            Assert.Skip("No en-US Win32 spell-check provider registered on this machine.");
        return checker!;
    }

    [Fact]
    public void MisspelledWordIsFlaggedAndCorrectWordIsNot()
    {
        var checker = RequireChecker();

        const string text = "hello wrrrong";
        var boundaries = Boundaries(text);
        var corrections = checker.SpellCheck(boundaries, text);

        Assert.Equal(2, corrections.Count);
        Assert.Null(corrections[0]);
        Assert.NotNull(corrections[1]);

        // Ranges are relative to boundaries[i-1] (the previous word's end, so they
        // include any whitespace before this word) — that is exactly the origin
        // FlowDocumentView.RefreshSpellCheckSquiggles adds them to when placing a
        // squiggle. Assert the absolute span they resolve to, which is what has to
        // line up with the rendered glyphs.
        var (start, end) = corrections[1]!.Value;
        int origin = boundaries[0];
        Assert.Equal(text.IndexOf("wrrrong", StringComparison.Ordinal), origin + start);
        Assert.Equal(text.Length, origin + end);
    }

    [Fact]
    public void AllCorrectWordsProduceNoCorrections()
    {
        var checker = RequireChecker();

        const string text = "the quick brown fox";
        var corrections = checker.SpellCheck(Boundaries(text), text);

        Assert.Equal(4, corrections.Count);
        Assert.All(corrections, Assert.Null);
    }

    [Fact]
    public void EmptyInputsReturnEmptyOrAllNullWithoutThrowing()
    {
        var checker = RequireChecker();

        Assert.Empty(checker.SpellCheck([], string.Empty));
        // Boundaries present but no text: one entry per word, all unflagged.
        Assert.All(checker.SpellCheck([5], string.Empty), Assert.Null);
    }

    [Fact]
    public void UnknownLanguageTagYieldsNoCheckerRatherThanThrowing()
    {
        // TryCreate must swallow the COM failure so spell-check degrades to
        // "no squiggles" instead of taking down layout.
        Assert.Null(MS.Internal.Documents.Win32SpellChecker.TryCreate("zz-ZZ"));
    }
#endif
}
