// Shim for the WPF-only DataGridExtensions NuGet package (auto-filter support for
// System.Windows.Controls.DataGrid). The real package targets WPF and will not restore on
// net*-desktop (Uno.Sdk), so this provides the small API surface the ILSpy metadata pane uses:
//
//   - IContentFilter / IContentFilterFactory / RegexContentFilterFactory
//   - DataGridFilter attached API (IsAutoFilterEnabled, ContentFilterFactory, GetFilter().Clear())
//   - DataGridColumn.SetTemplate(...) filter-column extension
//
// The interactive auto-filter UI is not rendered yet (the column filter ControlTemplate is stored
// but inert), mirroring the icon-pipeline approach: enough surface to compile and host data, with
// the live filtering behavior deferred. Replace storage with real filtering when the metadata pane
// gains its filter controls.

using System;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

using System.Windows.Controls;

namespace DataGridExtensions
{
    /// <summary>A content filter evaluates whether a cell value matches the active filter.</summary>
    public interface IContentFilter
    {
        bool IsMatch(object? value);
    }

    /// <summary>Creates an <see cref="IContentFilter"/> for a given filter content (e.g. user text).</summary>
    public interface IContentFilterFactory
    {
        IContentFilter Create(object? content);
    }

    /// <summary>Default factory producing a case-insensitive regex/substring filter.</summary>
    public sealed class RegexContentFilterFactory : IContentFilterFactory
    {
        public IContentFilter Create(object? content) => new RegexContentFilter(content as string);

        private sealed class RegexContentFilter : IContentFilter
        {
            private readonly Regex? regex;

            public RegexContentFilter(string? pattern)
            {
                if (!string.IsNullOrEmpty(pattern))
                {
                    try
                    {
                        regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                    }
                    catch (ArgumentException)
                    {
                        regex = null;
                    }
                }
            }

            public bool IsMatch(object? value)
            {
                if (regex == null)
                    return true;
                return value != null && regex.IsMatch(value.ToString() ?? string.Empty);
            }
        }
    }

    /// <summary>Per-DataGrid filter host; <see cref="Clear"/> resets all active column filters.</summary>
    public interface IDataGridFilterHost
    {
        void Clear();
    }

    /// <summary>
    /// Attached-property facade matching DataGridExtensions.DataGridFilter. Values are stored per
    /// DataGrid; the live filter row is not yet rendered (deferred with the metadata pane UI).
    /// </summary>
    public static class DataGridFilter
    {
        private sealed class State : IDataGridFilterHost
        {
            public bool IsAutoFilterEnabled;
            public IContentFilterFactory? ContentFilterFactory;
            public void Clear() { }
        }

        private static readonly ConditionalWeakTable<DataGrid, State> states = new();

        private static State GetState(DataGrid grid) => states.GetValue(grid, _ => new State());

        public static void SetIsAutoFilterEnabled(DataGrid grid, bool value)
            => GetState(grid).IsAutoFilterEnabled = value;

        public static bool GetIsAutoFilterEnabled(DataGrid grid)
            => GetState(grid).IsAutoFilterEnabled;

        public static void SetContentFilterFactory(DataGrid grid, IContentFilterFactory value)
            => GetState(grid).ContentFilterFactory = value;

        public static IContentFilterFactory? GetContentFilterFactory(DataGrid grid)
            => GetState(grid).ContentFilterFactory;

        public static IDataGridFilterHost GetFilter(DataGrid grid) => GetState(grid);
    }

    /// <summary>
    /// Filter-column facade matching DataGridExtensions.DataGridFilterColumn. The ILSpy metadata
    /// pane calls <c>column.SetTemplate(...)</c> to choose the per-column filter editor template.
    /// </summary>
    public static class DataGridFilterColumn
    {
        private static readonly ConditionalWeakTable<DataGridColumn, System.Windows.Controls.ControlTemplate> templates = new();

        public static void SetTemplate(this DataGridColumn column, System.Windows.Controls.ControlTemplate? value)
        {
            if (value == null)
                templates.Remove(column);
            else
                templates.AddOrUpdate(column, value);
        }

        public static System.Windows.Controls.ControlTemplate? GetTemplate(this DataGridColumn column)
            => templates.TryGetValue(column, out var t) ? t : null;
    }
}
