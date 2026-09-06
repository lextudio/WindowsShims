using System;
using System.Windows.Documents;
using MS.Internal;
using Windows.Foundation;
using Xunit;

namespace LeXtudio.Windows.Tests;

/// <summary>
/// Covers the fixed-document (XPS) spine now linked from upstream WPF:
/// the DocumentPage/DocumentPaginator family and MS.Internal.ContentType.
/// </summary>
public sealed class FixedDocumentSpineTests
{
    [Fact]
    public void MissingPageHasNoVisualAndEmptySize()
    {
        var page = DocumentPage.Missing;

        Assert.Null(page.Visual);
        Assert.Equal(Size.Empty, page.Size);
    }

    [Fact]
    public void BleedAndContentBoxDefaultToThePageRect()
    {
        var size = new Size(816, 1056);
        var page = new DocumentPage(null!, size, Rect.Empty, Rect.Empty);

        Assert.Equal(size, page.Size);
        Assert.Equal(new Rect(new Point(), size), page.BleedBox);
        Assert.Equal(new Rect(new Point(), size), page.ContentBox);
    }

    [Fact]
    public void ExplicitBleedAndContentBoxAreKept()
    {
        var size = new Size(816, 1056);
        var bleed = new Rect(5, 5, 806, 1046);
        var content = new Rect(48, 48, 720, 960);
        var page = new DocumentPage(null!, size, bleed, content);

        Assert.Equal(bleed, page.BleedBox);
        Assert.Equal(content, page.ContentBox);
    }

    [Fact]
    public void PaginatorRejectsNegativePageNumbers()
    {
        var paginator = new StubPaginator();

        Assert.Throws<ArgumentOutOfRangeException>(() => paginator.GetPageAsync(-1));
    }

    [Fact]
    public void GetPageAsyncRaisesGetPageCompleted()
    {
        var paginator = new StubPaginator();
        GetPageCompletedEventArgs? seen = null;
        paginator.GetPageCompleted += (_, e) => seen = e;

        paginator.GetPageAsync(2, "state");

        Assert.NotNull(seen);
        Assert.Equal(2, seen!.PageNumber);
        Assert.Equal("state", seen.UserState);
        Assert.Same(paginator.Page, seen.DocumentPage);
    }

    [Fact]
    public void DynamicPaginatorRaisesGetPageNumberCompleted()
    {
        var paginator = new StubDynamicPaginator();
        GetPageNumberCompletedEventArgs? seen = null;
        paginator.GetPageNumberCompleted += (_, e) => seen = e;

        var position = new StubContentPosition();
        paginator.GetPageNumberAsync(position, "state");

        Assert.NotNull(seen);
        Assert.Equal(7, seen!.PageNumber);
        Assert.Same(position, seen.ContentPosition);
        Assert.Equal("state", seen.UserState);
    }

    [Fact]
    public void DynamicPaginatorRejectsMissingContentPosition()
    {
        var paginator = new StubDynamicPaginator();

        Assert.Throws<ArgumentException>(() => paginator.GetPageNumberAsync(ContentPosition.Missing));
    }

    [Theory]
    [InlineData("application/xaml+xml", "application", "xaml+xml")]
    [InlineData("Application/XAML+XML", "application", "xaml+xml")]
    [InlineData("text/plain; charset=utf-8", "text", "plain")]
    public void ContentTypeParsesTypeAndSubType(string value, string type, string subType)
    {
        var contentType = new ContentType(value);

        Assert.Equal(type, contentType.TypeComponent, ignoreCase: true);
        Assert.Equal(subType, contentType.SubTypeComponent, ignoreCase: true);
    }

    [Fact]
    public void ContentTypeComparisonIsCaseInsensitive()
    {
        Assert.True(new ContentType("TEXT/PLAIN").AreTypeAndSubTypeEqual(new ContentType("text/plain")));
    }

    [Fact]
    public void ContentTypeComparisonRejectsParametersUnlessAllowed()
    {
        var withParameters = new ContentType("text/plain; charset=utf-8");
        var bare = new ContentType("TEXT/PLAIN");

        // Upstream semantics: a parameter on either side fails the weak comparison
        // unless the caller opts in with allowParameterValuePairs.
        Assert.False(bare.AreTypeAndSubTypeEqual(withParameters));
        Assert.True(bare.AreTypeAndSubTypeEqual(withParameters, allowParameterValuePairs: true));
    }

    [Fact]
    public void ContentTypeRejectsMalformedValue()
    {
        Assert.Throws<ArgumentException>(() => new ContentType("not-a-content-type"));
    }

    private sealed class StubContentPosition : ContentPosition
    {
    }

    private sealed class StubPaginator : DocumentPaginator
    {
        public DocumentPage Page { get; } = new(null!, new Size(100, 200), Rect.Empty, Rect.Empty);

        public override bool IsPageCountValid => true;

        public override int PageCount => 3;

        public override Size PageSize { get; set; } = new(100, 200);

        public override IDocumentPaginatorSource? Source => null;

        public override DocumentPage GetPage(int pageNumber) => Page;
    }

    private sealed class StubDynamicPaginator : DynamicDocumentPaginator
    {
        public override bool IsPageCountValid => true;

        public override int PageCount => 3;

        public override Size PageSize { get; set; } = new(100, 200);

        public override IDocumentPaginatorSource? Source => null;

        public override DocumentPage GetPage(int pageNumber) => DocumentPage.Missing;

        public override int GetPageNumber(ContentPosition contentPosition) => 7;

        public override ContentPosition GetPagePosition(DocumentPage page) => ContentPosition.Missing;

        public override ContentPosition GetObjectPosition(object value) => ContentPosition.Missing;
    }
}

/// <summary>Element-level shims the fixed-document sources need (rung 4).</summary>
public sealed class FixedDocumentElementShimTests
{
    [Fact]
    public void XmlLanguageConvertsImplicitlyFromAnIetfTag()
    {
        // WinUI types FrameworkElement.Language as string; WPF types it as XmlLanguage.
        System.Windows.Markup.XmlLanguage language = "en-US";

        Assert.Equal("en-US", language.IetfLanguageTag);
        Assert.Equal("en-US", language.GetSpecificCulture().Name);
    }

    [Fact]
    public void XmlLanguageFallsBackToInvariantForAMalformedTag()
    {
        // ICU accepts a surprising range of tags, so this uses characters no tag may hold.
        System.Windows.Markup.XmlLanguage language = "!!not a tag!!";

        Assert.Same(System.Globalization.CultureInfo.InvariantCulture, language.GetSpecificCulture());
    }

    [Fact]
    public void InheritanceBehaviorDefaultsToDefault()
    {
        Assert.Equal(0, (int)default(System.Windows.InheritanceBehavior));
        Assert.Equal(System.Windows.InheritanceBehavior.Default, default);
    }
}
