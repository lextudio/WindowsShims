using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace LeXtudio.Windows.Sample;

public sealed partial class ScenarioHost : UserControl
{
    private static readonly string? _scenariosSource;
    private static readonly string _xamlSource;

    static ScenarioHost()
    {
        _xamlSource = """
<Page x:Class="LeXtudio.Windows.Sample.MainPage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Grid x:Name="Root">
        <NavigationView x:Name="NavView" PaneDisplayMode="Left"
                        SelectionChanged="OnScenarioSelected">
            <NavigationView.Header>
                <StackPanel Orientation="Horizontal" Spacing="16">
                    <TextBlock Text="LeXtudio.Windows DataGrid"
                               VerticalAlignment="Center" FontSize="18" />
                    <Button x:Name="ThemeButton" Click="OnToggleTheme">
                        <StackPanel Orientation="Horizontal" Spacing="6">
                            <FontIcon Glyph="&#xE793;" />
                            <TextBlock Text="Toggle Theme" />
                        </StackPanel>
                    </Button>
                    <DropDownButton>
                        <StackPanel Orientation="Horizontal" Spacing="6">
                            <PathIcon Data="..." />
                            <TextBlock Text="Source" />
                        </StackPanel>
                    </DropDownButton>
                </StackPanel>
            </NavigationView.Header>
            <ContentControl x:Name="ScenarioContent" />
        </NavigationView>
        <StackPanel x:Name="LoadingOverlay" Visibility="Collapsed"
                    VerticalAlignment="Center" HorizontalAlignment="Center">
            <ProgressRing IsActive="True" Width="50" Height="50" />
            <TextBlock Text="Preparing demo data..." />
        </StackPanel>
    </Grid>
</Page>
""";

        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream("LeXtudio.Windows.Sample.Resources.DataGridScenarios.cs");
        if (stream is not null)
        {
            using var reader = new StreamReader(stream);
            _scenariosSource = reader.ReadToEnd();
        }
    }

    public ScenarioHost()
    {
        InitializeComponent();
        ActualThemeChanged += (_, _) => ApplyShellTheme();
        ApplyShellTheme();
    }

    public void Show(string title, string description, UIElement grid, UIElement? options = null)
    {
        TitleText.Text = title;
        DescriptionText.Text = description;
        GridHost.Content = grid;

        OptionsPanel.Children.Clear();
        OptionsFrame.Visibility = options is not null ? Visibility.Visible : Visibility.Collapsed;

        if (options is not null)
        {
            OptionsPanel.Children.Add(options);
        }

        PopulateSourceCode();
        ApplyShellTheme();
    }

    private void ApplyShellTheme()
    {
        var dark = ActualTheme == ElementTheme.Dark || RequestedTheme == ElementTheme.Dark;
        GridFrame.Background = MainPage.Brush(dark ? 0x4C3A3A3Au : 0x80FFFFFFu);
        GridFrame.BorderBrush = MainPage.Brush(dark ? 0x19FFFFFFu : 0x0F000000u);
        OptionsFrame.Background = MainPage.Brush(dark ? 0x4C3A3A3Au : 0x80FFFFFFu);
        OptionsFrame.BorderBrush = MainPage.Brush(dark ? 0x19FFFFFFu : 0x0F000000u);
    }

    private void OnSourceExpanderExpanded(Expander sender, ExpanderExpandingEventArgs args)
    {
        sourceRow.Height = new GridLength(1, GridUnitType.Star);
    }

    private void OnSourceExpanderCollapsed(Expander sender, ExpanderCollapsedEventArgs args)
    {
        sourceRow.Height = GridLength.Auto;
    }

    private void PopulateSourceCode()
    {
        SourcePivot.Items.Clear();
        var hasAny = false;

        if (!string.IsNullOrWhiteSpace(_xamlSource))
        {
            hasAny = true;
            AddCodeTab(_xamlSource, "XAML");
        }

        if (!string.IsNullOrWhiteSpace(_scenariosSource))
        {
            hasAny = true;
            AddCodeTab(_scenariosSource, "C#");
        }

        SourceExpander.Visibility = hasAny ? Visibility.Visible : Visibility.Collapsed;
    }

    private void AddCodeTab(string code, string lang)
    {
        var textBlock = new TextBlock
        {
            Text = code,
            Margin = new Thickness(0, 8, 0, 0),
            FontFamily = new FontFamily("Consolas"),
            TextWrapping = TextWrapping.NoWrap,
            IsTextSelectionEnabled = true,
        };

        SourcePivot.Items.Add(new PivotItem
        {
            Header = lang,
            Content = new ScrollViewer
            {
                HorizontalScrollMode = ScrollMode.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollMode = ScrollMode.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = textBlock,
            },
        });
    }
}
