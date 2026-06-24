// Shim for the WPF-only DataGridExtensions NuGet package (auto-filter support for
// System.Windows.Controls.DataGrid). The real package targets WPF and will not restore on
// net*-desktop (Uno.Sdk), so this provides the full API surface the ILSpy metadata pane uses:
//
//   - IContentFilter / IContentFilterFactory / RegexContentFilterFactory
//   - DataGridFilter attached API (IsAutoFilterEnabled, ContentFilterFactory, GetFilter().Clear())
//   - DataGridColumn.SetTemplate(...) filter-column extension
//   - FilterControlTemplate — ControlTemplate subclass carrying FilterKind + FlagsType so
//     BuildFilterRow() in DataGrid.cs can instantiate the right Uno filter control per column.
//
// Filter kinds:
//   Text  — plain case-insensitive substring TextBox (default)
//   Hex   — "0x" prefix TextBox; matches against "{value:x8}"
//   Flags — ToggleButton + Flyout + CheckBox list; uses MaskContentFilter

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

    // ── Filter types ──────────────────────────────────────────────────────────

    public enum FilterKind { Text, Hex, Flags }

    /// <summary>
    /// A ControlTemplate subclass (so WPF cast succeeds) carrying enough metadata for
    /// BuildFilterRow() to create the right Uno filter control without WPF XAML.
    /// </summary>
    public sealed class FilterControlTemplate : System.Windows.Controls.ControlTemplate
    {
        public FilterKind Kind { get; }
        public Type? FlagsType { get; }

        public FilterControlTemplate(FilterKind kind, Type? flagsType = null)
            : base(typeof(System.Windows.Controls.Primitives.DataGridColumnHeader))
        {
            Kind = kind;
            FlagsType = flagsType;
        }
    }

    /// <summary>Matches when "{value:x8}" contains the filter string (case-insensitive).</summary>
    public sealed class HexContentFilter : IContentFilter
    {
        private readonly string _text;
        public string Text => _text;

        public HexContentFilter(string text) => _text = text;

        public bool IsMatch(object? value)
        {
            if (string.IsNullOrWhiteSpace(_text)) return true;
            if (value is null) return false;
            return string.Format("{0:x8}", value)
                         .IndexOf(_text, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }

    /// <summary>Matches when (mask == -1) or ((int)value &amp; mask) != 0.</summary>
    public sealed class MaskContentFilter : IContentFilter
    {
        public int Mask { get; }
        public MaskContentFilter(int mask) => Mask = mask;

        public bool IsMatch(object? value)
        {
            if (value is null) return true;
            int v;
            try { v = Convert.ToInt32(value); } catch { return true; }
            return Mask == -1 || (Mask & v) != 0;
        }
    }

    /// <summary>Case-insensitive substring match.</summary>
    public sealed class SubstringContentFilter : IContentFilter
    {
        private readonly string _text;
        public string Text => _text;
        public SubstringContentFilter(string text) => _text = text;
        public bool IsMatch(object? value)
            => value is not null &&
               (value.ToString() ?? string.Empty)
               .Contains(_text, StringComparison.OrdinalIgnoreCase);
    }

    // ── Per-DataGrid filter host ───────────────────────────────────────────────

    /// <summary>Per-DataGrid filter host; <see cref="Clear"/> resets all active column filters.</summary>
    public interface IDataGridFilterHost
    {
        void Clear();
    }

    /// <summary>
    /// Attached-property facade matching DataGridExtensions.DataGridFilter. Stores per-column
    /// IContentFilter instances and applies them via <see cref="MatchesAllFilters"/>.
    /// </summary>
    public static class DataGridFilter
    {
        internal sealed class State : IDataGridFilterHost
        {
            private readonly DataGrid _grid;
            public bool IsAutoFilterEnabled;
            public IContentFilterFactory? ContentFilterFactory;
            // keyed by column; value is the active IContentFilter (null = no filter)
            internal readonly System.Collections.Generic.Dictionary<DataGridColumn, IContentFilter?> ColumnFilters = new();
            internal readonly System.Collections.Generic.Dictionary<DataGridColumn, string> ColumnFilterText = new();

            internal State(DataGrid grid) => _grid = grid;

            public void Clear()
            {
                ColumnFilters.Clear();
                ColumnFilterText.Clear();
                _grid.BuildShimVisualTree();
            }
        }

        private static readonly ConditionalWeakTable<DataGrid, State> states = new();

        internal static State GetState(DataGrid grid) => states.GetValue(grid, g => new State(g));

        public static void SetIsAutoFilterEnabled(DataGrid grid, bool value)
            => GetState(grid).IsAutoFilterEnabled = value;

        public static bool GetIsAutoFilterEnabled(DataGrid grid)
            => GetState(grid).IsAutoFilterEnabled;

        public static void SetContentFilterFactory(DataGrid grid, IContentFilterFactory value)
            => GetState(grid).ContentFilterFactory = value;

        public static IContentFilterFactory? GetContentFilterFactory(DataGrid grid)
            => GetState(grid).ContentFilterFactory;

        public static IDataGridFilterHost GetFilter(DataGrid grid) => GetState(grid);

        /// <summary>
        /// Returns true if <paramref name="item"/> passes all active column filters for
        /// <paramref name="grid"/>. Always true when auto-filter is disabled or no filters active.
        /// </summary>
        public static bool MatchesAllFilters(DataGrid grid, object? item)
        {
            if (item is null) return true;
            var state = GetState(grid);
            if (!state.IsAutoFilterEnabled || state.ColumnFilters.Count == 0) return true;
            foreach (var (column, filter) in state.ColumnFilters)
            {
                if (filter is null) continue;
                string? path = column is DataGridBoundColumn bound ? bound.BindingPath : null;
                object? cellValue = path is { Length: > 0 }
                    ? item.GetType().GetProperty(path)?.GetValue(item)
                    : null;
                if (!filter.IsMatch(cellValue))
                    return false;
            }
            return true;
        }
    }

    // ── Per-column template ───────────────────────────────────────────────────

    /// <summary>
    /// Filter-column facade matching DataGridExtensions.DataGridFilterColumn. The ILSpy metadata
    /// pane calls <c>column.SetTemplate(...)</c> to choose the per-column filter editor template.
    /// When the value is a <see cref="FilterControlTemplate"/>, BuildFilterRow() uses its Kind and
    /// FlagsType to build the correct Uno filter control.
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
