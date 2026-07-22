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
        public int Quantity { get; set; }
        public string UnitPrice { get; set; } = "";
    }

    public sealed class MasterRow
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string OrderedOn { get; set; } = "";
        public string Status { get; set; } = "";
        public string Total { get; set; } = "";
        public List<DetailRow> Details { get; } = new();
    }

    public sealed class VariableHeightRow
    {
        public int Id { get; set; }
        public bool IsTall { get; set; }
        public string Service { get; set; } = "";
        public string Status { get; set; } = "";
        public string Owner { get; set; } = "";
        public string Summary { get; set; } = "";
    }

    public sealed class GroupedRow
    {
        public string Country { get; set; } = "";
        public string Name { get; set; } = "";
        public string Role { get; set; } = "";
        public string Office { get; set; } = "";
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
                nestedGrid.Columns.Add(new WpfDataGridTextColumn { Header = "Line", Binding = new WpfBinding("Id"), Width = new System.Windows.Controls.DataGridLength(60) });
                nestedGrid.Columns.Add(new WpfDataGridTextColumn { Header = "Product", Binding = new WpfBinding("Value"), Width = new System.Windows.Controls.DataGridLength(180) });
                nestedGrid.Columns.Add(new WpfDataGridTextColumn { Header = "Qty", Binding = new WpfBinding("Quantity"), Width = new System.Windows.Controls.DataGridLength(60) });
                nestedGrid.Columns.Add(new WpfDataGridTextColumn { Header = "Unit price", Binding = new WpfBinding("UnitPrice"), Width = new System.Windows.Controls.DataGridLength(90) });
                nestedGrid.CanUserAddRows = false;
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

            return new System.Windows.Controls.ShimDataTemplate(dataContext => new Border
            {
                MinHeight = 150,
                Padding = new Thickness(16, 10),
                Background = new SolidColorBrush(global::Windows.UI.Color.FromArgb(0xFF, 0xDD, 0xEE, 0xFF)),
                Child = new StackPanel
                {
                    Spacing = 4,
                    Children =
                    {
                        new TextBlock { Text = "Incident context", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold },
                        new TextBlock { Text = (dataContext as VariableHeightRow)?.Summary ?? "", TextWrapping = TextWrapping.Wrap },
                        new TextBlock { Text = "Regional failover notes and recovery status.", Opacity = 0.7 },
                    },
                },
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
        var grid = new WpfDataGrid { AutoGenerateColumns = false, CanUserAddRows = false };
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Order", Binding = new WpfBinding("Id"), Width = new System.Windows.Controls.DataGridLength(80) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Customer", Binding = new WpfBinding("Name"), Width = new System.Windows.Controls.DataGridLength(180) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Ordered", Binding = new WpfBinding("OrderedOn"), Width = new System.Windows.Controls.DataGridLength(110) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Status", Binding = new WpfBinding("Status"), Width = new System.Windows.Controls.DataGridLength(100) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Total", Binding = new WpfBinding("Total"), Width = new System.Windows.Controls.DataGridLength(90) });

        string[] customers = ["Northwind Traders", "Contoso Retail", "Fabrikam Labs", "Adventure Works", "Tailspin Toys"];
        string[] products = ["Surface Dock", "Ergonomic Keyboard", "USB-C Hub", "27-inch Display"];
        grid.ItemsSource = Enumerable.Range(1, 5).Select(i => new MasterRow
        {
            Id = 1040 + i,
            Name = customers[i - 1],
            OrderedOn = "Jul " + (10 + i) + ", 2026",
            Status = i % 3 == 0 ? "On hold" : i % 2 == 0 ? "Shipped" : "Processing",
            Total = "$" + (i * 428 + 129).ToString("N0"),
            Details =
            {
                new DetailRow { Id = i * 10 + 1, Value = products[(i - 1) % products.Length], Quantity = i + 1, UnitPrice = "$129.00" },
                new DetailRow { Id = i * 10 + 2, Value = products[i % products.Length], Quantity = 1, UnitPrice = "$299.00" },
            }
        }).ToList();
        grid.RowDetailsVisibilityMode = System.Windows.Controls.DataGridRowDetailsVisibilityMode.Visible;
        grid.RowDetailsTemplateSelector = new RowDetailsSelector();

        return grid;
    }

    public static WpfDataGrid BuildVariableHeightGrid(int rowCount, int tallRowIndex)
    {
        var grid = new WpfDataGrid { AutoGenerateColumns = false, CanUserAddRows = false };
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Check", Binding = new WpfBinding("Id"), Width = new System.Windows.Controls.DataGridLength(70) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Service", Binding = new WpfBinding("Service"), Width = new System.Windows.Controls.DataGridLength(180) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Status", Binding = new WpfBinding("Status"), Width = new System.Windows.Controls.DataGridLength(100) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Owner", Binding = new WpfBinding("Owner"), Width = new System.Windows.Controls.DataGridLength(130) });

        grid.ItemsSource = Enumerable.Range(0, rowCount)
            .Select(i => new VariableHeightRow
            {
                Id = 7000 + i,
                IsTall = i == tallRowIndex,
                Service = new[] { "Identity API", "Billing worker", "Search index", "Notification hub" }[i % 4],
                Status = i == tallRowIndex ? "Incident" : i % 5 == 0 ? "Investigating" : "Healthy",
                Owner = new[] { "Platform", "Commerce", "Discovery", "Messaging" }[i % 4],
                Summary = i == tallRowIndex
                    ? "Elevated latency followed a regional failover. Traffic is stable while cache recovery completes."
                    : "Routine service health check.",
            })
            .ToList();
        grid.RowDetailsVisibilityMode = System.Windows.Controls.DataGridRowDetailsVisibilityMode.Visible;
        grid.RowDetailsTemplateSelector = new VariableHeightDetailsSelector();

        return grid;
    }

    public static WpfDataGrid BuildGroupedStyleGrid(string mode)
    {
        var grid = new WpfDataGrid { AutoGenerateColumns = false, CanUserAddRows = false };
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Country", Binding = new WpfBinding("Country"), Width = new System.Windows.Controls.DataGridLength(80) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Name", Binding = new WpfBinding("Name"), Width = new System.Windows.Controls.DataGridLength(100) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Role", Binding = new WpfBinding("Role"), Width = new System.Windows.Controls.DataGridLength(150) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Office", Binding = new WpfBinding("Office"), Width = new System.Windows.Controls.DataGridLength(120) });

        string[] countries = ["Canada", "Germany", "Japan", "United States"];
        string[][] namesByCountry =
        [
            ["Amelia Tremblay", "Liam Chen", "Sophie Gagnon", "Noah Singh", "Maya Campbell", "Émile Roy", "Olivia Martin", "Ethan Wong"],
            ["Anna Schneider", "Lukas Müller", "Leonie Fischer", "Felix Wagner", "Clara Hoffmann", "Jonas Becker", "Mia Schäfer", "Niklas Weber"],
            ["Yui Sato", "Haruto Suzuki", "Aoi Takahashi", "Ren Tanaka", "Sakura Watanabe", "Kaito Ito", "Mei Yamamoto", "Sota Nakamura"],
            ["Ava Johnson", "Mateo Garcia", "Chloe Williams", "Elijah Brown", "Zoe Davis", "Jackson Wilson", "Camila Martinez", "Henry Anderson"],
        ];
        string[][] officesByCountry =
        [
            ["Toronto", "Vancouver", "Montréal", "Calgary"],
            ["Berlin", "Munich", "Hamburg", "Cologne"],
            ["Tokyo", "Osaka", "Kyoto", "Fukuoka"],
            ["Seattle", "New York", "Austin", "Chicago"],
        ];
        string[] roles = ["Designer", "Engineer", "Product manager", "Researcher"];
        grid.ItemsSource = Enumerable.Range(0, 32).Select(i =>
        {
            var countryIndex = i / 8;
            var personIndex = i % 8;
            return new GroupedRow
            {
                Country = countries[countryIndex],
                Name = namesByCountry[countryIndex][personIndex],
                Role = roles[(personIndex + countryIndex) % roles.Length],
                Office = officesByCountry[countryIndex][personIndex % 4],
            };
        }).ToList();
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
            .Select(i => new FrozenEditRow
            {
                ColA = "SKU-" + (1200 + i),
                ColB = new[] { "Wireless keyboard", "USB-C dock", "Studio headset", "Travel mouse" }[i % 4],
                ColC = new[] { "Toronto", "Seattle", "Berlin", "Tokyo" }[i % 4],
                ColD = (24 + i * 3).ToString(),
            })
            .ToList();

        var grid = new WpfDataGrid { AutoGenerateColumns = false, ItemsSource = rows, FrozenColumnCount = 2 };
        var columns = new[]
        {
            ("ColA", "SKU", 150d),
            ("ColB", "Product", 240d),
            ("ColC", "Warehouse", 200d),
            ("ColD", "On hand", 180d),
        };
        foreach (var (propertyName, header, width) in columns)
        {
            grid.Columns.Add(new WpfDataGridTextColumn
            {
                Header = header,
                Binding = new WpfBinding(propertyName),
                Width = new System.Windows.Controls.DataGridLength(width),
            });
        }

        return grid;
    }
}
