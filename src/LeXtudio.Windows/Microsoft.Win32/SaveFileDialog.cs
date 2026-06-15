// WPF-API shim for the common file dialogs. The real WPF SaveFileDialog/OpenFileDialog
// (PresentationFramework Microsoft/Win32/*.cs) are Win32 native-coupled (HwndWrapper,
// IFileDialog COM interop) and cannot be linked on Skia/macOS. This minimal shim mirrors
// the public WPF API surface so upstream ILSpy code compiles and runs headless.

using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Win32
{
    public abstract class FileDialog
    {
        public string FileName { get; set; } = string.Empty;
        public string[] FileNames { get; set; } = [];
        public string Filter { get; set; } = string.Empty;
        public int FilterIndex { get; set; } = 1;
        public string DefaultExt { get; set; } = string.Empty;
        public string InitialDirectory { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public object? Owner { get; set; }
        public bool? DialogResult { get; set; }

        public virtual bool? ShowDialog() => false;

        public virtual Task<bool?> ShowDialogAsync() => Task.FromResult(ShowDialog());

        public Stream OpenFile()
            => new FileStream(FileName, FileMode.Create, FileAccess.ReadWrite);
    }

    public class SaveFileDialog : FileDialog
    {
    }

    public class OpenFileDialog : FileDialog
    {
        public bool Multiselect { get; set; }
    }
}
