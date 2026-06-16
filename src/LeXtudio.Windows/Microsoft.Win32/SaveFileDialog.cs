// WPF-API shim for the common file dialogs. The real WPF SaveFileDialog/OpenFileDialog
// (PresentationFramework Microsoft/Win32/*.cs) are Win32 native-coupled (HwndWrapper,
// IFileDialog COM interop) and cannot be linked on Skia/macOS. This shim mirrors the
// public WPF API surface but is implemented on top of the Uno/WinUI pickers
// (Windows.Storage.Pickers.FileOpenPicker / FileSavePicker / FolderPicker) so upstream
// code that constructs these dialogs runs on every Uno target.
//
// Pickers are inherently asynchronous; prefer ShowDialogAsync(). The synchronous
// ShowDialog() is provided for WPF source compatibility and blocks on the async picker,
// so it must not be called on the UI thread (it would deadlock waiting for a picker that
// needs that same thread to pump). New Roma code should await ShowDialogAsync().

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

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

        // Synchronous WPF entry point. Blocks on the async picker; do not call on the UI thread.
        public bool? ShowDialog() => ShowDialogAsync().GetAwaiter().GetResult();

        // WPF overload that takes an owner window.
        public bool? ShowDialog(object? owner)
        {
            Owner = owner;
            return ShowDialog();
        }

        public abstract Task<bool?> ShowDialogAsync();

        public Stream OpenFile()
            => new FileStream(FileName, FileMode.Create, FileAccess.ReadWrite);

        // Parses a WPF filter string ("Desc|*.cs;*.csx|All files|*.*") into the distinct
        // extension tokens the WinUI pickers expect (".cs", ".csx", or "*").
        private protected static IReadOnlyList<string> ExtensionsFromFilter(string filter)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(filter))
                return result;

            var parts = filter.Split('|');
            // Odd entries (index 1, 3, …) hold the patterns; even entries hold descriptions.
            for (int i = 1; i < parts.Length; i += 2)
            {
                foreach (var token in parts[i].Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    var ext = NormalizeExtension(token.Trim());
                    if (ext.Length > 0 && !result.Contains(ext))
                        result.Add(ext);
                }
            }
            return result;
        }

        // "*.cs" -> ".cs"; "*.*"/"*" -> "*"; ".cs" -> ".cs".
        private protected static string NormalizeExtension(string token)
        {
            if (token is "*.*" or "*" or "")
                return "*";
            int dot = token.LastIndexOf('.');
            if (dot < 0)
                return "." + token;
            var ext = token[dot..];
            return ext == ".*" ? "*" : ext;
        }
    }

    public class OpenFileDialog : FileDialog
    {
        public bool Multiselect { get; set; }

        public override async Task<bool?> ShowDialogAsync()
        {
            var picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.ComputerFolder,
                ViewMode = PickerViewMode.List,
            };

            var extensions = ExtensionsFromFilter(Filter);
            if (extensions.Count == 0)
                picker.FileTypeFilter.Add("*");
            else
                foreach (var ext in extensions)
                    picker.FileTypeFilter.Add(ext);

            FileDialogHost.InitializeWithActiveWindow(picker);

            if (Multiselect)
            {
                var files = await picker.PickMultipleFilesAsync();
                if (files is null || files.Count == 0)
                {
                    DialogResult = false;
                    return false;
                }
                FileNames = files.Select(f => f.Path).ToArray();
                FileName = FileNames[0];
                DialogResult = true;
                return true;
            }

            var file = await picker.PickSingleFileAsync();
            if (file is null)
            {
                DialogResult = false;
                return false;
            }
            FileName = file.Path;
            FileNames = [file.Path];
            DialogResult = true;
            return true;
        }
    }

    public class SaveFileDialog : FileDialog
    {
        public bool OverwritePrompt { get; set; } = true;
        public bool AddExtension { get; set; } = true;

        public override async Task<bool?> ShowDialogAsync()
        {
            var picker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.ComputerFolder,
            };

            // FileSavePicker requires at least one named choice. Group the parsed extensions
            // under the original filter descriptions so the WPF FilterIndex still lines up.
            var choices = BuildSaveChoices(Filter);
            foreach (var (description, exts) in choices)
                picker.FileTypeChoices[description] = exts;
            if (picker.FileTypeChoices.Count == 0)
                picker.FileTypeChoices["All files"] = new List<string> { "." };

            if (!string.IsNullOrEmpty(FileName))
                picker.SuggestedFileName = Path.GetFileName(FileName);

            FileDialogHost.InitializeWithActiveWindow(picker);

            var file = await picker.PickSaveFileAsync();
            if (file is null)
            {
                DialogResult = false;
                return false;
            }
            FileName = file.Path;
            FileNames = [file.Path];
            DialogResult = true;
            return true;
        }

        // Maps the WPF filter pairs to FileSavePicker choices. FileSavePicker rejects the
        // wildcard "*" extension, so "*.*" entries are represented by the catch-all ".".
        private static List<(string Description, List<string> Extensions)> BuildSaveChoices(string filter)
        {
            var choices = new List<(string, List<string>)>();
            if (string.IsNullOrWhiteSpace(filter))
                return choices;

            var parts = filter.Split('|');
            for (int i = 0; i + 1 < parts.Length; i += 2)
            {
                var description = parts[i].Trim();
                var exts = new List<string>();
                foreach (var token in parts[i + 1].Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    var ext = NormalizeExtension(token.Trim());
                    exts.Add(ext == "*" ? "." : ext);
                }
                if (description.Length == 0)
                    description = "Files";
                if (exts.Count > 0)
                    choices.Add((description, exts));
            }
            return choices;
        }
    }

    // WPF (.NET 8+) Microsoft.Win32.OpenFolderDialog, backed by the WinUI FolderPicker.
    public class OpenFolderDialog : FileDialog
    {
        public bool Multiselect { get; set; }
        public string FolderName { get; set; } = string.Empty;
        public string[] FolderNames { get; set; } = [];

        public override async Task<bool?> ShowDialogAsync()
        {
            var picker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.ComputerFolder,
            };
            // FolderPicker requires at least one file-type filter to be set.
            picker.FileTypeFilter.Add("*");

            FileDialogHost.InitializeWithActiveWindow(picker);

            var folder = await picker.PickSingleFolderAsync();
            if (folder is null)
            {
                DialogResult = false;
                return false;
            }
            FolderName = folder.Path;
            FolderNames = [folder.Path];
            FileName = folder.Path;
            FileNames = [folder.Path];
            DialogResult = true;
            return true;
        }
    }
}
