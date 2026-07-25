using System.Windows.Controls;
using Xunit;

namespace LeXtudio.Windows.Tests;

public sealed class WpfXamlResourceTranslatorTests
{
    [Fact]
    public void TranslateResourceDictionaryReadsStyleAndFilterTemplates()
    {
        const string xaml = """
            <ResourceDictionary
                xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                xmlns:srm="clr-namespace:System.Reflection;assembly=System.Reflection.Metadata">
                <Style x:Key="DataGridCellStyle" TargetType="{x:Type DataGridCell}">
                    <Setter Property="BorderThickness" Value="0" />
                    <Setter Property="Padding" Value="2" />
                    <Setter Property="VerticalContentAlignment" Value="Center" />
                </Style>
                <ControlTemplate x:Key="DefaultFilter">
                    <Grid><TextBox /></Grid>
                </ControlTemplate>
                <ControlTemplate x:Key="AssemblyFlagsFilter">
                    <local:FlagsFilterControl xmlns:local="clr-namespace:Local" FlagsType="{x:Type srm:AssemblyFlags}" />
                </ControlTemplate>
            </ResourceDictionary>
            """;

        var specs = WpfXamlResourceTranslator.TranslateResourceDictionary(xaml, ResolveType);

        Assert.Equal(new[]
        {
            "DataGridCellStyle",
            "DefaultFilter",
            "AssemblyFlagsFilter"
        }, specs.Select(spec => spec.Key));

        var flagsTemplate = (DataGridExtensions.FilterControlTemplate)specs[2].CreateValue();
        Assert.Equal(DataGridExtensions.FilterKind.Flags, flagsTemplate.Kind);
        Assert.Equal(typeof(System.Reflection.AssemblyFlags), flagsTemplate.FlagsType);
    }

    [Fact]
    public void TranslateResourceDictionaryAppendsFallbackForUnsupportedResources()
    {
        const string xaml = """
            <ResourceDictionary
                xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                <DataTemplate x:Key="Template" />
            </ResourceDictionary>
            """;

        var specs = WpfXamlResourceTranslator.TranslateResourceDictionary(
            xaml,
            ResolveType,
            WpfResourceSpec.Value("Template", "fallback"));

        Assert.Equal(1, specs.Length);
        Assert.Equal("Template", specs[0].Key);
        Assert.Equal("fallback", specs[0].CreateValue());
    }

    [Fact]
    public void TranslateResourceDictionaryReportsTranslatedFallbackAndSkippedKeys()
    {
        const string xaml = """
            <ResourceDictionary
                xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                <Style x:Key="ItemContainerStyle" TargetType="ListViewItem">
                    <Setter Property="HorizontalContentAlignment" Value="Stretch" />
                </Style>
                <local:Unsupported xmlns:local="clr-namespace:Local" x:Key="Unsupported" />
                <DataTemplate x:Key="Template" />
            </ResourceDictionary>
            """;

        _ = WpfXamlResourceTranslator.TranslateResourceDictionary(
            xaml,
            ResolveType,
            out var report,
            WpfResourceSpec.Value("Template", "fallback"));

        Assert.Equal(new[] { "ItemContainerStyle" }, report.TranslatedKeys);
        Assert.Equal(new[] { "Template" }, report.FallbackKeys);
        Assert.Equal(new[] { "Unsupported", "Template" }, report.SkippedKeys);
    }

    [Fact]
    public void TranslateResourceDictionaryReadsStyleBasedOnAndStaticResourceSetter()
    {
        const string xaml = """
            <ResourceDictionary
                xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                <Style x:Key="BaseCellStyle" TargetType="{x:Type DataGridCell}">
                    <Setter Property="Padding" Value="2" />
                </Style>
                <Style x:Key="DataGridCellStyle" TargetType="{x:Type DataGridCell}" BasedOn="{StaticResource BaseCellStyle}">
                    <Setter Property="Tag" Value="{StaticResource MissingValue}" />
                    <Setter Property="Template">
                        <Setter.Value>
                            <ControlTemplate TargetType="{x:Type DataGridCell}" />
                        </Setter.Value>
                    </Setter>
                </Style>
            </ResourceDictionary>
            """;

        var specs = WpfXamlResourceTranslator.TranslateResourceDictionary(xaml, ResolveType);

        Assert.Equal(new[] { "BaseCellStyle", "DataGridCellStyle" }, specs.Select(spec => spec.Key));
        var styleSpec = (StyleSpec)specs[1].Descriptor!;
        var templateSetter = styleSpec.Setters.SingleOrDefault(setter => setter.PropertyName == "Template");
        Assert.IsType<System.Windows.Controls.ControlTemplate>(templateSetter?.Value);
        Assert.Equal(typeof(DataGridCell), ((IWpfTemplateBridge)templateSetter!.Value!).TargetType);
    }

    [Fact]
    public void TranslateResourceDictionaryReadsKeyedObjectResource()
    {
        const string xaml = """
            <ResourceDictionary
                xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                xmlns:local="clr-namespace:Local">
                <local:SharedConverter x:Key="converter" />
            </ResourceDictionary>
            """;

        var specs = WpfXamlResourceTranslator.TranslateResourceDictionary(xaml, ResolveType, out var report);

        Assert.Equal(new[] { "converter" }, specs.Select(spec => spec.Key));
        Assert.Same(SharedConverter.Instance, specs[0].CreateValue());
        Assert.Equal(new[] { "converter" }, report.TranslatedKeys);
        Assert.Empty(report.SkippedKeys);
    }

    [Fact]
    public void DataTemplateSpecExposesResourceDictionaryFactoryOverload()
    {
        var overload = typeof(WpfResourceSpec).GetMethods()
            .SingleOrDefault(method =>
                method.Name == nameof(WpfResourceSpec.DataTemplate)
                && method.GetParameters() is { Length: 2 } parameters
                && parameters[1].ParameterType.GenericTypeArguments.FirstOrDefault() == typeof(System.Windows.ResourceDictionary));

        Assert.NotNull(overload);
    }

    [Fact]
    public void TranslateResourceDictionaryReadsSimpleTextBoxAndDataGridDataTemplates()
    {
        const string xaml = """
            <ResourceDictionary
                xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                <DataTemplate x:Key="TextBlob">
                    <Grid MinWidth="800" MaxWidth="800" HorizontalAlignment="Left">
                        <TextBox IsReadOnly="True" TextWrapping="Wrap" Text="{Binding RowDetails, Mode=OneWay}"
                                 MinLines="10" MaxLines="25" />
                    </Grid>
                </DataTemplate>
                <DataTemplate x:Key="GridDetails">
                    <DataGrid ItemsSource="{Binding RowDetails}" />
                </DataTemplate>
            </ResourceDictionary>
            """;

        var specs = WpfXamlResourceTranslator.TranslateResourceDictionary(
            xaml,
            ResolveType,
            out var report);

        Assert.Equal(new[] { "TextBlob", "GridDetails" }, specs.Select(spec => spec.Key));
        Assert.Equal(new[] { "TextBlob", "GridDetails" }, report.TranslatedKeys);
        Assert.Empty(report.FallbackKeys);
    }

    [Fact]
    public void TranslateResourceDictionaryReadsImplicitDataTemplateKey()
    {
        const string xaml = """
            <ResourceDictionary
                xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                xmlns:local="clr-namespace:Local">
                <DataTemplate DataType="{x:Type local:SampleRow}">
                    <TextBox Text="{Binding Name}" />
                </DataTemplate>
            </ResourceDictionary>
            """;

        var specs = WpfXamlResourceTranslator.TranslateResourceDictionary(
            xaml,
            ResolveType,
            out var report);

        Assert.Equal(1, specs.Length);
        Assert.Equal(typeof(SampleRow), specs[0].Key);
        Assert.Equal(new[] { typeof(SampleRow).FullName }, report.TranslatedKeys);
    }

    [Fact]
    public void TranslateResourceDictionaryReadsResourcesFromControlRoot()
    {
        const string xaml = """
            <Control
                xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                xmlns:local="clr-namespace:Local">
                <Control.Resources>
                    <local:SharedConverter x:Key="nullVisConv" />
                    <DataTemplate DataType="{x:Type local:SampleRow}">
                        <TextBox Text="{Binding Name}" />
                    </DataTemplate>
                </Control.Resources>
                <Control.Template>
                    <ControlTemplate />
                </Control.Template>
            </Control>
            """;

        var specs = WpfXamlResourceTranslator.TranslateResourceDictionary(
            xaml,
            ResolveType,
            out var report);

        Assert.Equal(new object[] { "nullVisConv", typeof(SampleRow) }, specs.Select(spec => spec.Key));
        Assert.Same(SharedConverter.Instance, specs[0].CreateValue());
        Assert.Equal(new[] { "nullVisConv", typeof(SampleRow).FullName }, report.TranslatedKeys);
        Assert.Empty(report.SkippedKeys);
    }

    [Fact]
    public void TranslateResourceDictionaryReadsNestedGridResourcesAndContextMenuSetter()
    {
        const string xaml = """
            <UserControl
                xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                <Grid>
                    <Grid.Resources>
                        <AlternationConverter x:Key="BackgroundConverter">
                            <SolidColorBrush Color="Transparent" />
                            <SolidColorBrush Color="#CCCC33" Opacity="0.15" />
                        </AlternationConverter>
                        <Style x:Key="alternatingWithBinding"
                               TargetType="{x:Type ListViewItem}" BasedOn="{StaticResource {x:Type ListViewItem}}">
                            <Setter Property="Background"
                                    Value="{Binding RelativeSource={RelativeSource Self}, Path=(ItemsControl.AlternationIndex), Converter={StaticResource BackgroundConverter}}" />
                            <Setter Property="ContextMenu">
                                <Setter.Value>
                                    <ContextMenu>
                                        <MenuItem Header="_Copy" Command="ApplicationCommands.Copy" />
                                        <MenuItem Header="Copy _value" Command="ApplicationCommands.Copy" CommandParameter="Value" InputGestureText=" " />
                                    </ContextMenu>
                                </Setter.Value>
                            </Setter>
                        </Style>
                    </Grid.Resources>
                </Grid>
            </UserControl>
            """;

        var specs = WpfXamlResourceTranslator.TranslateResourceDictionary(
            xaml,
            ResolveType,
            out var report);

        Assert.Equal(new[] { "BackgroundConverter", "alternatingWithBinding" }, specs.Select(spec => spec.Key));
        var converterSpec = (AlternationConverterSpec)specs[0].Descriptor!;
        Assert.Equal(2, converterSpec.Values.Count);
        Assert.Equal(0.15, converterSpec.Values[1].Opacity);
        var styleSpec = (StyleSpec)specs[1].Descriptor!;
        Assert.Equal(typeof(Microsoft.UI.Xaml.Controls.ListViewItem), styleSpec.TargetType);
        Assert.Equal("ListViewItem", styleSpec.BasedOnReference?.Key);
        Assert.IsType<System.Windows.Data.Binding>(styleSpec.Setters.Single(setter => setter.PropertyName == "Background").Value);
        var contextMenu = (ContextMenuSpec)styleSpec.Setters.Single(setter => setter.PropertyName == "ContextMenu").Value!;
        Assert.Equal(2, contextMenu.Items.Count);
        Assert.Equal("Value", contextMenu.Items[1].CommandParameter);
        Assert.Equal(new[] { "BackgroundConverter", "alternatingWithBinding" }, report.TranslatedKeys);
        Assert.Empty(report.SkippedKeys);
    }

    [Fact]
    public void TranslateResourceDictionaryReadsStackPanelTextBlockDataTemplate()
    {
        const string xaml = """
            <ResourceDictionary
                xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                xmlns:local="clr-namespace:Local">
                <DataTemplate DataType="{x:Type local:ChoiceRow}">
                    <StackPanel Orientation="Horizontal" Margin="3">
                        <TextBlock Text="{Binding Header}" FontWeight="Bold" />
                        <TextBlock Text="{Binding SelectedFlag.Name}" />
                    </StackPanel>
                </DataTemplate>
            </ResourceDictionary>
            """;

        var specs = WpfXamlResourceTranslator.TranslateResourceDictionary(
            xaml,
            ResolveType,
            out var report);

        Assert.Equal(new object[] { typeof(ChoiceRow) }, specs.Select(spec => spec.Key));
        Assert.Empty(report.SkippedKeys);

        Assert.Equal(new[] { typeof(ChoiceRow).FullName }, report.TranslatedKeys);
    }

    [Fact]
    public void TranslateResourceDictionaryReadsListBoxCheckBoxDataTemplate()
    {
        const string xaml = """
            <ResourceDictionary
                xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                xmlns:local="clr-namespace:Local">
                <DataTemplate DataType="{x:Type local:MultiChoiceRow}">
                    <StackPanel Orientation="Vertical" Margin="3">
                        <TextBlock Text="{Binding Header}" FontWeight="Bold" Margin="0 0 0 3" />
                        <ListBox ItemsSource="{Binding Flags}" BorderThickness="0" Background="Transparent">
                            <ListBox.ItemTemplate>
                                <DataTemplate>
                                    <CheckBox DockPanel.Dock="Left" Margin="3,2" Content="{Binding Name}"
                                              IsChecked="{Binding IsSelected, Mode=OneWay}"/>
                                </DataTemplate>
                            </ListBox.ItemTemplate>
                        </ListBox>
                    </StackPanel>
                </DataTemplate>
            </ResourceDictionary>
            """;

        var specs = WpfXamlResourceTranslator.TranslateResourceDictionary(
            xaml,
            ResolveType,
            out var report);

        Assert.Equal(new object[] { typeof(MultiChoiceRow) }, specs.Select(spec => spec.Key));
        Assert.Equal(new[] { typeof(MultiChoiceRow).FullName }, report.TranslatedKeys);
        Assert.Empty(report.SkippedKeys);
    }

    private static Type? ResolveType(string name)
        => name switch
        {
            "DataGridCell" => typeof(DataGridCell),
            "ListViewItem" => typeof(Microsoft.UI.Xaml.Controls.ListViewItem),
            "srm:AssemblyFlags" => typeof(System.Reflection.AssemblyFlags),
            "SharedConverter" or "local:SharedConverter" => typeof(SharedConverter),
            "local:SampleRow" => typeof(SampleRow),
            "local:ChoiceRow" => typeof(ChoiceRow),
            "local:MultiChoiceRow" => typeof(MultiChoiceRow),
            _ => null
        };

    private sealed class SharedConverter
    {
        public static readonly SharedConverter Instance = new();
    }

    private sealed class SampleRow
    {
        public string? Name { get; set; }
    }

    private sealed record ChoiceRow(string Header, ChoiceFlag SelectedFlag);

    private sealed record ChoiceFlag(string Name);

    private sealed record MultiChoiceRow(string Header, ChoiceFlag[] Flags);
}
