using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WpfBinding = System.Windows.Data.Binding;
using WpfDataGrid = System.Windows.Controls.DataGrid;
using WpfDataGridTextColumn = System.Windows.Controls.DataGridTextColumn;
using WpfDataGridCheckBoxColumn = System.Windows.Controls.DataGridCheckBoxColumn;
using WpfDataGridComboBoxColumn = System.Windows.Controls.DataGridComboBoxColumn;
using WpfDataGridHyperlinkColumn = System.Windows.Controls.DataGridHyperlinkColumn;

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

    public sealed class SortableRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public double Price { get; set; }
        public int Stock { get; set; }
        public string Status { get; set; } = "";
    }

    public static WpfDataGrid BuildSortingGrid()
    {
        var grid = new WpfDataGrid
        {
            AutoGenerateColumns = false,
            CanUserSortColumns = true,
        };
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Id", Binding = new WpfBinding("Id"), Width = new System.Windows.Controls.DataGridLength(60) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Product", Binding = new WpfBinding("Name"), Width = new System.Windows.Controls.DataGridLength(180) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Category", Binding = new WpfBinding("Category"), Width = new System.Windows.Controls.DataGridLength(120) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Price", Binding = new WpfBinding("Price"), Width = new System.Windows.Controls.DataGridLength(90) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Stock", Binding = new WpfBinding("Stock"), Width = new System.Windows.Controls.DataGridLength(80) });

        string[] categories = ["Electronics", "Books", "Clothing", "Home", "Sports"];
        grid.ItemsSource = Enumerable.Range(1, 50).Select(i => new SortableRow
        {
            Id = i,
            Name = $"Product {i}",
            Category = categories[i % categories.Length],
            Price = Math.Round(9.99 + i * 1.45, 2),
            Stock = (i * 7) % 100,
        }).ToList();

        return grid;
    }

    public sealed class SelectableRow
    {
        public string Code { get; set; } = "";
        public string Title { get; set; } = "";
        public string Author { get; set; } = "";
        public int Year { get; set; }
        public string Format { get; set; } = "";
    }

    public static WpfDataGrid BuildSelectionGrid()
    {
        var grid = new WpfDataGrid
        {
            AutoGenerateColumns = false,
            SelectionMode = System.Windows.Controls.DataGridSelectionMode.Extended,
            SelectionUnit = System.Windows.Controls.DataGridSelectionUnit.CellOrRowHeader,
        };
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Code", Binding = new WpfBinding("Code"), Width = new System.Windows.Controls.DataGridLength(80) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Title", Binding = new WpfBinding("Title"), Width = new System.Windows.Controls.DataGridLength(250) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Author", Binding = new WpfBinding("Author"), Width = new System.Windows.Controls.DataGridLength(160) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Year", Binding = new WpfBinding("Year"), Width = new System.Windows.Controls.DataGridLength(70) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Format", Binding = new WpfBinding("Format"), Width = new System.Windows.Controls.DataGridLength(90) });

        grid.ItemsSource = Enumerable.Range(1, 30).Select(i => new SelectableRow
        {
            Code = $"REF-{1000 + i}",
            Title = new[] { "The Art of Programming", "Data Structures Unleashed", "Clean Architecture", "Design Patterns", "Functional Thinking" }[i % 5],
            Author = new[] { "Martin Fowler", "Robert C. Martin", "Erich Gamma", "Donald Knuth", "John Ousterhout" }[i % 5],
            Year = 2015 + i % 10,
            Format = new[] { "Hardcover", "Paperback", "eBook", "Audio" }[i % 4],
        }).ToList();

        return grid;
    }

    public static WpfDataGrid BuildReorderGrid()
    {
        var grid = new WpfDataGrid
        {
            AutoGenerateColumns = false,
            CanUserReorderColumns = true,
            CanUserResizeColumns = true,
        };
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Priority", Binding = new WpfBinding("Id"), Width = new System.Windows.Controls.DataGridLength(70) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Task", Binding = new WpfBinding("Name"), Width = new System.Windows.Controls.DataGridLength(200) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Assigned To", Binding = new WpfBinding("Category"), Width = new System.Windows.Controls.DataGridLength(140) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Status", Binding = new WpfBinding("Status"), Width = new System.Windows.Controls.DataGridLength(100) });

        grid.ItemsSource = Enumerable.Range(1, 15).Select(i => new SortableRow
        {
            Id = i,
            Name = new[] { "Review PR", "Update docs", "Fix bug #1234", "Deploy v2.1", "Write tests", "Refactor module", "Security audit", "Performance tuning", "API design review", "Code cleanup", "Update dependencies", "Migration script", "Database backup", "Monitor alert", "Release notes" }[i - 1],
            Category = new[] { "Alice", "Bob", "Charlie", "Diana", "Eve", "Frank", "Grace", "Henry", "Iris", "Jack", "Kate", "Leo", "Mia", "Noah", "Olivia" }[i - 1],
            Status = new[] { "In Progress", "Done", "Pending", "Blocked" }[i % 4],
        }).ToList();

        return grid;
    }

    public static WpfDataGrid BuildClipboardGrid()
    {
        var grid = new WpfDataGrid
        {
            AutoGenerateColumns = false,
            ClipboardCopyMode = System.Windows.Controls.DataGridClipboardCopyMode.IncludeHeader,
        };
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Id", Binding = new WpfBinding("Id"), Width = new System.Windows.Controls.DataGridLength(60) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Name", Binding = new WpfBinding("Name"), Width = new System.Windows.Controls.DataGridLength(180) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Category", Binding = new WpfBinding("Category"), Width = new System.Windows.Controls.DataGridLength(120) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Price", Binding = new WpfBinding("Price"), Width = new System.Windows.Controls.DataGridLength(90) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Stock", Binding = new WpfBinding("Stock"), Width = new System.Windows.Controls.DataGridLength(80) });

        string[] categories = ["Electronics", "Books", "Clothing", "Home", "Sports"];
        grid.ItemsSource = Enumerable.Range(1, 20).Select(i => new SortableRow
        {
            Id = i,
            Name = $"Item {i}",
            Category = categories[i % categories.Length],
            Price = Math.Round(9.99 + i * 1.45, 2),
            Stock = (i * 7) % 100,
        }).ToList();

        return grid;
    }

    public static WpfDataGrid BuildGridLinesGrid()
    {
        var grid = new WpfDataGrid
        {
            AutoGenerateColumns = false,
            GridLinesVisibility = System.Windows.Controls.DataGridGridLinesVisibility.Horizontal,
            HorizontalGridLinesBrush = new SolidColorBrush(global::Windows.UI.Color.FromArgb(0xFF, 0x00, 0x7A, 0xCC)),
        };
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Id", Binding = new WpfBinding("Id"), Width = new System.Windows.Controls.DataGridLength(60) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Service", Binding = new WpfBinding("Name"), Width = new System.Windows.Controls.DataGridLength(200) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Health", Binding = new WpfBinding("Category"), Width = new System.Windows.Controls.DataGridLength(120) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Uptime", Binding = new WpfBinding("Status"), Width = new System.Windows.Controls.DataGridLength(100) });

        grid.ItemsSource = Enumerable.Range(1, 12).Select(i => new SortableRow
        {
            Id = i,
            Name = new[] { "Web Server", "Database Primary", "Cache Cluster", "CDN Origin", "Auth Service", "API Gateway", "Search Index", "Queue Worker", "Log Aggregator", "Monitoring", "DNS Resolver", "Load Balancer" }[i - 1],
            Category = new[] { "Healthy", "Degraded", "Healthy", "Healthy", "Healthy", "Warning", "Healthy", "Healthy", "Degraded", "Healthy", "Healthy", "Healthy" }[i - 1],
            Status = new[] { "99.9%", "98.5%", "99.99%", "99.9%", "99.9%", "97.2%", "100%", "99.9%", "95.0%", "99.9%", "99.99%", "100%" }[i - 1],
        }).ToList();

        return grid;
    }

    public sealed class HeaderRow
    {
        public string ColA { get; set; } = "";
        public string ColB { get; set; } = "";
        public string ColC { get; set; } = "";
        public string ColD { get; set; } = "";
        public string ColE { get; set; } = "";
    }

    public static WpfDataGrid BuildHeadersGrid()
    {
        var grid = new WpfDataGrid
        {
            AutoGenerateColumns = false,
            HeadersVisibility = System.Windows.Controls.DataGridHeadersVisibility.Column,
        };
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "A", Binding = new WpfBinding("ColA"), Width = new System.Windows.Controls.DataGridLength(80) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "B", Binding = new WpfBinding("ColB"), Width = new System.Windows.Controls.DataGridLength(80) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "C", Binding = new WpfBinding("ColC"), Width = new System.Windows.Controls.DataGridLength(80) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "D", Binding = new WpfBinding("ColD"), Width = new System.Windows.Controls.DataGridLength(80) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "E", Binding = new WpfBinding("ColE"), Width = new System.Windows.Controls.DataGridLength(80) });

        grid.ItemsSource = Enumerable.Range(1, 10).Select(i => new HeaderRow
        {
            ColA = $"R{i}C1", ColB = $"R{i}C2", ColC = $"R{i}C3", ColD = $"R{i}C4", ColE = $"R{i}C5",
        }).ToList();

        return grid;
    }

    public sealed class TypedRow
    {
        public string Item { get; set; } = "";
        public bool InStock { get; set; }
        public string Category { get; set; } = "";
        public string Link { get; set; } = "";
    }

    public static WpfDataGrid BuildColumnTypesGrid()
    {
        var grid = new WpfDataGrid { AutoGenerateColumns = false, CanUserAddRows = false };
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Item", Binding = new WpfBinding("Item"), Width = new System.Windows.Controls.DataGridLength(160) });
        grid.Columns.Add(new WpfDataGridCheckBoxColumn { Header = "In Stock", Binding = new WpfBinding("InStock"), Width = new System.Windows.Controls.DataGridLength(80) });
        grid.Columns.Add(new WpfDataGridComboBoxColumn
        {
            Header = "Category",
            SelectedItemBinding = new WpfBinding("Category"),
            ItemsSource = new[] { "Electronics", "Books", "Clothing", "Home", "Sports" },
            Width = new System.Windows.Controls.DataGridLength(130),
        });
        grid.Columns.Add(new WpfDataGridHyperlinkColumn
        {
            Header = "Details",
            Binding = new WpfBinding("Link"),
            Width = new System.Windows.Controls.DataGridLength(200),
        });

        grid.ItemsSource = Enumerable.Range(1, 15).Select(i => new TypedRow
        {
            Item = new[] { "Wireless Mouse", "C# in Depth", "Running Shoes", "Desk Lamp", "Yoga Mat", "USB-C Hub", "Notebook Set", "Coffee Maker", "Backpack", "Sunglasses", "Water Bottle", "Plant Pot", "Wall Art", "Pillow Set", "Board Game" }[i - 1],
            InStock = i % 3 != 0,
            Category = new[] { "Electronics", "Books", "Sports", "Home", "Sports", "Electronics", "Books", "Home", "Clothing", "Clothing", "Sports", "Home", "Home", "Home", "Sports" }[i - 1],
            Link = new[] { "Details...", "Details...", "Details...", "Details...", "Details...", "Details...", "Details...", "Details...", "Details...", "Details...", "Details...", "Details...", "Details...", "Details...", "Details..." }[i - 1],
        }).ToList();

        return grid;
    }

    public sealed class SizedRow
    {
        public string Short { get; set; } = "";
        public string Medium { get; set; } = "";
        public string Long { get; set; } = "";
        public string Flexible { get; set; } = "";
    }

    public static WpfDataGrid BuildColumnSizingGrid()
    {
        var grid = new WpfDataGrid { AutoGenerateColumns = false };
        grid.Columns.Add(new WpfDataGridTextColumn
        {
            Header = "Fixed (100px)",
            Binding = new WpfBinding("Short"),
            Width = new System.Windows.Controls.DataGridLength(100),
        });
        grid.Columns.Add(new WpfDataGridTextColumn
        {
            Header = "Size to Header",
            Binding = new WpfBinding("Medium"),
            Width = System.Windows.Controls.DataGridLength.SizeToHeader,
        });
        grid.Columns.Add(new WpfDataGridTextColumn
        {
            Header = "Auto (*)",
            Binding = new WpfBinding("Long"),
            Width = new System.Windows.Controls.DataGridLength(1, System.Windows.Controls.DataGridLengthUnitType.Star),
        });
        grid.Columns.Add(new WpfDataGridTextColumn
        {
            Header = "Auto (2*)",
            Binding = new WpfBinding("Flexible"),
            Width = new System.Windows.Controls.DataGridLength(2, System.Windows.Controls.DataGridLengthUnitType.Star),
        });

        grid.ItemsSource = Enumerable.Range(1, 10).Select(i => new SizedRow
        {
            Short = $"#{i}",
            Medium = $"Item {i} description",
            Long = $"This is a longer description for item number {i} that demonstrates auto-sizing behavior across different column width modes.",
            Flexible = $"Flex cell {i}",
        }).ToList();

        return grid;
    }

    public static WpfDataGrid BuildAlternatingRowGrid()
    {
        var grid = new WpfDataGrid
        {
            AutoGenerateColumns = false,
            AlternatingRowBackground = new SolidColorBrush(global::Windows.UI.Color.FromArgb(0xFF, 0xF0, 0xF5, 0xFA)),
        };
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Rank", Binding = new WpfBinding("Id"), Width = new System.Windows.Controls.DataGridLength(60) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Player", Binding = new WpfBinding("Name"), Width = new System.Windows.Controls.DataGridLength(180) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Score", Binding = new WpfBinding("Price"), Width = new System.Windows.Controls.DataGridLength(90) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Level", Binding = new WpfBinding("Category"), Width = new System.Windows.Controls.DataGridLength(100) });

        grid.ItemsSource = Enumerable.Range(1, 20).Select(i => new SortableRow
        {
            Id = i,
            Name = new[] { "Alpha", "Beta", "Gamma", "Delta", "Epsilon", "Zeta", "Eta", "Theta", "Iota", "Kappa", "Lambda", "Mu", "Nu", "Xi", "Omicron", "Pi", "Rho", "Sigma", "Tau", "Upsilon" }[i - 1],
            Price = Math.Round(5000.0 - i * 47.3, 1),
            Category = new[] { "Pro", "Advanced", "Intermediate", "Beginner" }[i % 4],
        }).ToList();

        return grid;
    }

    public sealed class LargeDataRow
    {
        public long Id { get; set; }
        public string Label { get; set; } = "";
        public string Group { get; set; } = "";
        public double Value { get; set; }
        public string Notes { get; set; } = "";
    }

    public static WpfDataGrid BuildLargeDataGrid()
    {
        var grid = new WpfDataGrid
        {
            AutoGenerateColumns = false,
            CanUserAddRows = false,
            CanUserSortColumns = true,
            EnableRowVirtualization = true,
            EnableColumnVirtualization = true,
        };
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Id", Binding = new WpfBinding("Id"), Width = new System.Windows.Controls.DataGridLength(80) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Label", Binding = new WpfBinding("Label"), Width = new System.Windows.Controls.DataGridLength(150) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Group", Binding = new WpfBinding("Group"), Width = new System.Windows.Controls.DataGridLength(100) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Value", Binding = new WpfBinding("Value"), Width = new System.Windows.Controls.DataGridLength(90) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Notes", Binding = new WpfBinding("Notes"), Width = new System.Windows.Controls.DataGridLength(300) });

        string[] groups = ["Alpha", "Beta", "Gamma", "Delta"];
        grid.ItemsSource = Enumerable.Range(1, 10_000).Select(i => new LargeDataRow
        {
            Id = i,
            Label = $"Item-{i:D5}",
            Group = groups[i % groups.Length],
            Value = Math.Round(new Random(i).NextDouble() * 1000, 2),
            Notes = i % 5 == 0 ? "Priority review required for this batch entry." : "Standard processing",
        }).ToList();

        return grid;
    }
}
