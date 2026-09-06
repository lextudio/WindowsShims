// The WPF application-model layer the fixed-document sources reach for when they load
// a page or resolve a relative uri.
//
// The upstream files (PresentationCore's BaseUriHelper and WpfWebRequestHelper, Shared's
// MimeTypeMapper) were tried as links and rejected: they pull in the whole pack:// URI
// and deployment stack — PackWebRequestFactory, SiteOfOriginContainer, CookieHandler,
// CustomCredentialPolicy, SafeSecurityHelper and MS.Win32.Compile — most of which is
// Windows-specific and none of which exists on Uno. Only the members the fixed-document
// sources actually call are provided here, with upstream's constant values and tree-walk
// semantics preserved.

using System.IO;
using System.IO.Packaging;
using System.Net;
using MS.Internal;
using System.Windows.Markup;

namespace System.Windows.Navigation
{
    /// <summary>
    /// Resolves the base uri an element's relative uris are relative to.
    /// </summary>
    public static class BaseUriHelper
    {
        // Upstream: PackUriHelper.Create(new Uri("application://")) and ("SiteOfOrigin://").
        private static readonly Uri PackApplicationBaseUri = PackUriHelper.Create(new Uri("application://"));
        private static readonly Uri PackSiteOfOriginBaseUri = PackUriHelper.Create(new Uri("SiteOfOrigin://"));

        /// <summary>
        /// The base uri an element's relative uris resolve against. Set by the XAML parser
        /// in WPF; here it is whatever the host assigns.
        /// </summary>
        public static readonly Microsoft.UI.Xaml.DependencyProperty BaseUriProperty =
            Microsoft.UI.Xaml.DependencyProperty.RegisterAttached(
                "BaseUri",
                typeof(Uri),
                typeof(BaseUriHelper),
                new System.Windows.FrameworkPropertyMetadata(null));

        /// <summary>pack://application:,,, — the application resource root.</summary>
        internal static Uri PackAppBaseUri => PackApplicationBaseUri;

        /// <summary>pack://siteoforigin:,,, — the deployment root.</summary>
        internal static Uri SiteOfOriginBaseUri => PackSiteOfOriginBaseUri;

        /// <summary>
        /// The application-wide base uri relative uris resolve against when no element
        /// supplies one. Upstream defaults it to the pack application root.
        /// </summary>
        internal static Uri BaseUri { get; set; } = PackApplicationBaseUri;

        /// <summary>
        /// Upstream FixFileUri: a file uri whose OriginalString does not start with the
        /// scheme is re-created from its AbsoluteUri so comparisons behave.
        /// </summary>
        internal static Uri? FixFileUri(Uri? uri)
        {
            if (uri is not null && uri.IsAbsoluteUri
                && string.Equals(uri.Scheme, Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase)
                && string.Compare(uri.OriginalString, 0, Uri.UriSchemeFile, 0, Uri.UriSchemeFile.Length, StringComparison.OrdinalIgnoreCase) != 0)
            {
                return new Uri(uri.AbsoluteUri);
            }

            return uri;
        }

        public static Uri? GetBaseUri(Microsoft.UI.Xaml.DependencyObject element)
            => GetBaseUriCore(element);

        public static void SetBaseUri(Microsoft.UI.Xaml.DependencyObject element, Uri? baseUri)
        {
            ArgumentNullException.ThrowIfNull(element);
            element.SetValue(BaseUriProperty, baseUri);
        }

        /// <summary>
        /// Walks up from <paramref name="element"/> for the nearest BaseUri value or
        /// IUriContext, matching upstream's GetBaseUriCore. WPF walks the UIElement,
        /// ContentElement and Visual parents in turn; on Uno the first two collapse into
        /// FrameworkContentElement.Parent and the visual parent.
        /// </summary>
        internal static Uri? GetBaseUriCore(Microsoft.UI.Xaml.DependencyObject element)
        {
            ArgumentNullException.ThrowIfNull(element);

            Microsoft.UI.Xaml.DependencyObject? current = element;

            while (current is not null)
            {
                if (current.GetValue(BaseUriProperty) is Uri fromProperty)
                {
                    return fromProperty;
                }

                if (current is IUriContext { BaseUri: { } fromContext })
                {
                    return fromContext;
                }

                current = current is FrameworkContentElement contentElement
                    ? contentElement.Parent
                    : Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(current);
            }

            return null;
        }
    }
}

namespace MS.Internal
{
    /// <summary>
    /// The media types the fixed-document sources compare against. Values are upstream's.
    /// </summary>
    internal static class MimeTypeMapper
    {
        internal static readonly ContentType XamlMime = new("application/xaml+xml");
        internal static readonly ContentType BamlMime = new("application/baml+xml");
        internal static readonly ContentType FixedDocumentSequenceMime =
            new("application/vnd.ms-package.xps-fixeddocumentsequence+xml");
        internal static readonly ContentType FixedDocumentMime =
            new("application/vnd.ms-package.xps-fixeddocument+xml");
        internal static readonly ContentType FixedPageMime =
            new("application/vnd.ms-package.xps-fixedpage+xml");
    }

    /// <summary>
    /// Fetches the bytes behind a uri. Upstream routes pack:// uris through
    /// PackWebRequestFactory and adds cookie and credential policy for http; here pack://
    /// is served from the in-process <see cref="PackageStore"/> and everything else goes
    /// through WebRequest with no cookie or credential handling.
    /// </summary>
    internal static class WpfWebRequestHelper
    {
        internal static Stream CreateRequestAndGetResponseStream(Uri uri)
            => CreateRequestAndGetResponseStream(uri, out _);

        internal static Stream CreateRequestAndGetResponseStream(Uri uri, out ContentType contentType)
        {
            ArgumentNullException.ThrowIfNull(uri);

            if (uri.IsAbsoluteUri && string.Equals(uri.Scheme, PackUriHelper.UriSchemePack, StringComparison.OrdinalIgnoreCase))
            {
                return GetPackPartStream(uri, out contentType);
            }

            var request = WebRequest.Create(uri);
            var response = request.GetResponse();
            contentType = string.IsNullOrEmpty(response.ContentType)
                ? ContentType.Empty
                : new ContentType(response.ContentType);
            return response.GetResponseStream();
        }

        private static Stream GetPackPartStream(Uri uri, out ContentType contentType)
        {
            var packageUri = PackUriHelper.GetPackageUri(uri);
            var partUri = PackUriHelper.GetPartUri(uri);

            var package = PackageStore.GetPackage(packageUri)
                ?? throw new IOException($"No package is registered for '{packageUri}'.");

            if (partUri is null || !package.PartExists(partUri))
            {
                throw new FileNotFoundException($"'{uri}' was not found in the package.");
            }

            var part = package.GetPart(partUri);
            contentType = string.IsNullOrEmpty(part.ContentType)
                ? ContentType.Empty
                : new ContentType(part.ContentType);
            return part.GetStream(FileMode.Open, FileAccess.Read);
        }
    }
}

namespace System.Windows.Markup
{
    /// <summary>
    /// Upstream XamlReaderHelper is 6800 lines of BAML/XAML parser. Only the one namespace
    /// constant the fixed-document schema check reads is carried here.
    /// </summary>
    internal static class XamlReaderHelper
    {
        internal const string DefinitionMetroNamespaceURI =
            "http://schemas.microsoft.com/xps/2005/06/resourcedictionary-key";
    }
}
