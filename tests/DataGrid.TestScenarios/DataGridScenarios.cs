using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WpfBinding = System.Windows.Data.Binding;
using WpfDataGrid = System.Windows.Controls.DataGrid;
using WpfDataGridTextColumn = System.Windows.Controls.DataGridTextColumn;

namespace DataGrid.TestScenarios;

public static class DataGridScenarios
{
    public sealed class MetadataRow
    {
        public int RID { get; set; }
        public string Token { get; set; } = "";
        public string Offset { get; set; } = "";
        public string Attributes { get; set; } = "";
        public string Name { get; set; } = "";
        public string Namespace { get; set; } = "";
        public string BaseType { get; set; } = "";
        public MetadataRowOwner? Owner { get; set; }
    }

    public sealed class MetadataRowOwner
    {
        public string Name { get; set; } = "";
    }

    public sealed class DetailRow
    {
        public int Id { get; set; }
        public string? Value { get; set; }
    }

    public sealed class MasterRow
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public List<DetailRow> Details { get; } = new();
    }

    public sealed class VariableHeightRow
    {
        public int Id { get; set; }
        public bool IsTall { get; set; }
    }

    public sealed class GroupedRow
    {
        public string Country { get; set; } = "";
        public string Name { get; set; } = "";
    }

    public sealed class FrozenEditRow
    {
        public string ColA { get; set; } = "";
        public string ColB { get; set; } = "";
        public string ColC { get; set; } = "";
        public string ColD { get; set; } = "";
    }

    public sealed class RecordingHeaderTemplateSelector : DataTemplateSelector
    {
        public static object? LastGroup { get; private set; }
        public static readonly DataTemplate Selected = new();

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            LastGroup = item;
            return Selected;
        }
    }

    public sealed class RecordingContainerStyleSelector : System.Windows.Controls.StyleSelector
    {
        public static object? LastGroup { get; private set; }
        public static readonly Style Selected = new(typeof(System.Windows.Controls.GroupItem));

        public override Style SelectStyle(object item, DependencyObject container)
        {
            LastGroup = item;
            return Selected;
        }
    }

    private sealed class RowDetailsSelector : DataTemplateSelector
    {
        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            return new System.Windows.Controls.ShimDataTemplate(dataContext =>
            {
                var nestedGrid = new WpfDataGrid { AutoGenerateColumns = false };
                nestedGrid.Columns.Add(new WpfDataGridTextColumn { Header = "DetailId", Binding = new WpfBinding("Id"), Width = new System.Windows.Controls.DataGridLength(50) });
                nestedGrid.Columns.Add(new WpfDataGridTextColumn { Header = "DetailValue", Binding = new WpfBinding("Value"), Width = new System.Windows.Controls.DataGridLength(100) });
                if (dataContext is MasterRow master)
                {
                    nestedGrid.ItemsSource = master.Details;
                }

                return nestedGrid;
            });
        }
    }

    private sealed class VariableHeightDetailsSelector : DataTemplateSelector
    {
        protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is not VariableHeightRow { IsTall: true })
            {
                return null;
            }

            return new System.Windows.Controls.ShimDataTemplate(_ => new Border
            {
                Height = 150,
                Background = new SolidColorBrush(global::Windows.UI.Color.FromArgb(0xFF, 0xDD, 0xEE, 0xFF)),
            });
        }
    }

    public static WpfDataGrid BuildMetadataGrid()
    {
        var grid = new WpfDataGrid { AutoGenerateColumns = false };
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "RID", Binding = new WpfBinding("RID"), Width = new System.Windows.Controls.DataGridLength(50) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Token", Binding = new WpfBinding("Token"), Width = new System.Windows.Controls.DataGridLength(80) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Offset", Binding = new WpfBinding("Offset"), Width = new System.Windows.Controls.DataGridLength(70) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Attributes", Binding = new WpfBinding("Attributes"), Width = new System.Windows.Controls.DataGridLength(90) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Name", Binding = new WpfBinding("Name"), Width = new System.Windows.Controls.DataGridLength(100) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Namespace", Binding = new WpfBinding("Namespace"), Width = new System.Windows.Controls.DataGridLength(120) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "BaseType", Binding = new WpfBinding("BaseType"), Width = new System.Windows.Controls.DataGridLength(80) });

        grid.ItemsSource = Enumerable.Range(1, 20).Select(i => new MetadataRow
        {
            RID = i,
            Token = $"0x0200000{i:X1}",
            Offset = $"0x{i * 4:X4}",
            Attributes = i % 2 == 0 ? "Public" : "Private",
            Name = $"Type{i}",
            Namespace = i < 10 ? "Root" : "Root.Sub",
            BaseType = i % 3 == 0 ? "object" : "ValueType",
            Owner = new MetadataRowOwner { Name = $"Owner{i}" },
        }).ToList();

        return grid;
    }

    public static WpfDataGrid BuildFilterGrid()
    {
        var grid = BuildMetadataGrid();
        DataGridExtensions.DataGridFilter.SetIsAutoFilterEnabled(grid, true);
        foreach (var col in grid.Columns.Cast<System.Windows.Controls.DataGridColumn>())
        {
            DataGridExtensions.DataGridFilterColumn.SetTemplate(col, new DataGridExtensions.FilterControlTemplate(DataGridExtensions.FilterKind.Text));
        }

        return grid;
    }

    public static WpfDataGrid BuildHexFilterGrid()
    {
        var grid = new WpfDataGrid { AutoGenerateColumns = false };
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "RID", Binding = new WpfBinding("RID"), Width = new System.Windows.Controls.DataGridLength(50) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Token", Binding = new WpfBinding("Token"), Width = new System.Windows.Controls.DataGridLength(80) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Offset", Binding = new WpfBinding("Offset"), Width = new System.Windows.Controls.DataGridLength(70) });

        grid.ItemsSource = Enumerable.Range(1, 20).Select(i => new MetadataRow
        {
            RID = i,
            Token = $"0x0200000{i:X1}",
            Offset = $"0x{i * 4:X4}",
            Name = $"Type{i}",
        }).ToList();

        DataGridExtensions.DataGridFilter.SetIsAutoFilterEnabled(grid, true);
        foreach (var col in grid.Columns.Cast<System.Windows.Controls.DataGridColumn>())
        {
            DataGridExtensions.DataGridFilterColumn.SetTemplate(col, new DataGridExtensions.FilterControlTemplate(DataGridExtensions.FilterKind.Hex));
        }

        return grid;
    }

    public static WpfDataGrid BuildRowDetailsGrid()
    {
        var grid = new WpfDataGrid { AutoGenerateColumns = false };
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Id", Binding = new WpfBinding("Id"), Width = new System.Windows.Controls.DataGridLength(50) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Name", Binding = new WpfBinding("Name"), Width = new System.Windows.Controls.DataGridLength(100) });

        grid.ItemsSource = Enumerable.Range(1, 5).Select(i => new MasterRow
        {
            Id = i,
            Name = $"Item{i}",
            Details = { new DetailRow { Id = i * 10 + 1, Value = $"detail-{i}-a" }, new DetailRow { Id = i * 10 + 2, Value = $"detail-{i}-b" } }
        }).ToList();
        grid.RowDetailsVisibilityMode = System.Windows.Controls.DataGridRowDetailsVisibilityMode.Visible;
        grid.RowDetailsTemplateSelector = new RowDetailsSelector();

        return grid;
    }

    public static WpfDataGrid BuildVariableHeightGrid(int rowCount, int tallRowIndex)
    {
        var grid = new WpfDataGrid { AutoGenerateColumns = false };
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Id", Binding = new WpfBinding("Id"), Width = new System.Windows.Controls.DataGridLength(60) });

        grid.ItemsSource = Enumerable.Range(0, rowCount)
            .Select(i => new VariableHeightRow { Id = i, IsTall = i == tallRowIndex })
            .ToList();
        grid.RowDetailsVisibilityMode = System.Windows.Controls.DataGridRowDetailsVisibilityMode.Visible;
        grid.RowDetailsTemplateSelector = new VariableHeightDetailsSelector();

        return grid;
    }

    public static WpfDataGrid BuildGroupedStyleGrid(string mode)
    {
        var grid = new WpfDataGrid { AutoGenerateColumns = false };
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Country", Binding = new WpfBinding("Country"), Width = new System.Windows.Controls.DataGridLength(80) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Name", Binding = new WpfBinding("Name"), Width = new System.Windows.Controls.DataGridLength(100) });

        grid.ItemsSource = new List<GroupedRow>
        {
            new() { Country = "US", Name = "Alice" },
            new() { Country = "US", Name = "Bob" },
        };
        grid.Items.GroupDescriptions.Add(new System.Windows.Data.PropertyGroupDescription("Country"));
        grid.Items.Refresh();

        switch (mode)
        {
            case "format":
                grid.GroupStyle.Add(new System.Windows.Controls.GroupStyle { HeaderStringFormat = "{0} ({1} people)" });
                break;

            case "selector":
                grid.GroupStyle.Add(new System.Windows.Controls.GroupStyle
                {
                    HeaderTemplate = new DataTemplate(),
                    HeaderTemplateSelector = new RecordingHeaderTemplateSelector(),
                    ContainerStyle = new Style(typeof(System.Windows.Controls.GroupItem)),
                    ContainerStyleSelector = new RecordingContainerStyleSelector(),
                });
                break;

            case "groupstyleselector":
                grid.GroupStyle.Add(new System.Windows.Controls.GroupStyle { HeaderStringFormat = "collection:{0}" });
                grid.GroupStyleSelector = (group, level) =>
                    new System.Windows.Controls.GroupStyle { HeaderStringFormat = "selector:{0}" };
                break;
        }

        return grid;
    }

    public static WpfDataGrid BuildFrozenEditGrid()
    {
        var rows = Enumerable.Range(0, 40)
            .Select(i => new FrozenEditRow { ColA = $"A{i}", ColB = $"B{i}", ColC = $"C{i}", ColD = $"D{i}" })
            .ToList();

        var grid = new WpfDataGrid { AutoGenerateColumns = false, ItemsSource = rows };
        foreach (var propertyName in new[] { "ColA", "ColB", "ColC", "ColD" })
        {
            grid.Columns.Add(new WpfDataGridTextColumn
            {
                Header = propertyName,
                Binding = new WpfBinding(propertyName),
                Width = new System.Windows.Controls.DataGridLength(220),
            });
        }

        return grid;
    }
}
