using System.Collections.Generic;
using System.IO;
using System.Text;

namespace System.Windows.Documents
{
    public sealed class RequestSetStatusBarEventArgs : System.Windows.RoutedEventArgs
    {
        public RequestSetStatusBarEventArgs(System.Uri? uri)
        {
            Uri = uri;
        }

        public static RequestSetStatusBarEventArgs Clear { get; } = new(null);
        public System.Uri? Uri { get; }
    }
}
