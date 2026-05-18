#if WINUI_BRIDGE
using Microsoft.UI.Xaml.Controls;

namespace System.Windows.Controls
{
    public class Image : Microsoft.UI.Xaml.FrameworkElement
    {
        public ImageSource Source { get; set; }

        public Image(Microsoft.UI.Xaml.Controls.Image image)
        {
            Source = null; // TODO: Convert Microsoft.UI.Xaml.Media.ImageSource to System.Windows.Media.ImageSource
        }

        public double Height {
            get => 0;
            set {
                // No-op, as we cannot set the height of the image source
            }
        }
        public double Width {
            get => 0;
            set {
                // No-op, as we cannot set the width of the image source
            }
        }
    }
}
#endif
