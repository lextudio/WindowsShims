using System;
using System.Windows.Navigation;
using MS.Internal;
using MS.Internal.Utility;
using Xunit;

namespace LeXtudio.Windows.Tests;

/// <summary>
/// The application-model layer the fixed-document sources load pages through (rung 5).
/// BindUriHelper is linked upstream; BaseUriHelper, MimeTypeMapper and WpfWebRequestHelper
/// are local because upstream drags in the pack:// deployment stack.
/// </summary>
public sealed class AppModelShimTests
{
    [Fact]
    public void AbsoluteUriResolvesToItself()
    {
        var absolute = new Uri("http://example.com/a/b.xaml");

        Assert.Equal(absolute, BindUriHelper.GetResolvedUri(null, absolute));
    }

    [Fact]
    public void RelativeUriResolvesAgainstTheBase()
    {
        var resolved = BindUriHelper.GetResolvedUri(
            new Uri("http://example.com/docs/index.xaml"),
            new Uri("pages/1.xaml", UriKind.Relative));

        Assert.Equal(new Uri("http://example.com/docs/pages/1.xaml"), resolved);
    }

    [Fact]
    public void NullUriResolvesToNull()
    {
        Assert.Null(BindUriHelper.GetResolvedUri(new Uri("http://example.com/"), null));
    }

    [Theory]
    [InlineData("http://example.com/a#frag", "frag")]
    [InlineData("http://example.com/a", "")]
    public void GetFragmentStripsTheHash(string uri, string expected)
    {
        Assert.Equal(expected, BindUriHelper.GetFragment(new Uri(uri)));
    }

    [Fact]
    public void GetFragmentWorksOnARelativeUri()
    {
        // Uri.Fragment is only defined for absolute uris, so upstream resolves relative
        // ones against a placebo base first.
        Assert.Equal("target", BindUriHelper.GetFragment(new Uri("page.xaml#target", UriKind.Relative)));
    }

    [Fact]
    public void StartWithFragmentDetectsAPureFragment()
    {
        Assert.True(BindUriHelper.StartWithFragment(new Uri("#target", UriKind.Relative)));
        Assert.False(BindUriHelper.StartWithFragment(new Uri("page.xaml#target", UriKind.Relative)));
    }

    [Theory]
    [InlineData("application/xaml+xml", true)]
    [InlineData("application/vnd.ms-package.xps-fixeddocumentsequence+xml", true)]
    [InlineData("application/vnd.ms-package.xps-fixeddocument+xml", true)]
    [InlineData("application/vnd.ms-package.xps-fixedpage+xml", true)]
    [InlineData("application/baml+xml", false)]
    [InlineData("image/png", false)]
    public void IsXamlMimeTypeRecognisesTheXpsMarkupTypes(string mimeType, bool expected)
    {
        Assert.Equal(expected, BindUriHelper.IsXamlMimeType(new ContentType(mimeType)));
    }

    [Fact]
    public void MimeTypeMapperCarriesTheUpstreamMediaTypes()
    {
        Assert.Equal("application/xaml+xml", MimeTypeMapper.XamlMime.ToString());
        Assert.Equal("application/baml+xml", MimeTypeMapper.BamlMime.ToString());
        Assert.Equal(
            "application/vnd.ms-package.xps-fixedpage+xml",
            MimeTypeMapper.FixedPageMime.ToString());
    }

    [Fact]
    public void PackBaseUrisUseTheApplicationAndSiteOfOriginAuthorities()
    {
        Assert.Equal("pack", BaseUriHelper.PackAppBaseUri.Scheme);
        Assert.StartsWith("pack://application:", BaseUriHelper.PackAppBaseUri.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith("pack://siteoforigin:", BaseUriHelper.SiteOfOriginBaseUri.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FixFileUriRebuildsAFileUriThatDoesNotStartWithItsScheme()
    {
        // A uri built from a bare path: OriginalString is the path, not "file:...".
        var uri = new Uri("/tmp/a.xaml", UriKind.RelativeOrAbsolute);
        if (!uri.IsAbsoluteUri)
        {
            return; // On a platform where the bare path is relative there is nothing to fix.
        }

        var fixedUri = BaseUriHelper.FixFileUri(uri);

        Assert.NotNull(fixedUri);
        Assert.StartsWith("file:", fixedUri!.OriginalString, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FixFileUriLeavesANonFileUriAlone()
    {
        var uri = new Uri("http://example.com/a.xaml");

        Assert.Same(uri, BaseUriHelper.FixFileUri(uri));
    }

    [Fact]
    public void XamlReaderHelperCarriesTheMetroDefinitionNamespace()
    {
        Assert.Equal(
            "http://schemas.microsoft.com/xps/2005/06/resourcedictionary-key",
            System.Windows.Markup.XamlReaderHelper.DefinitionMetroNamespaceURI);
    }
}
