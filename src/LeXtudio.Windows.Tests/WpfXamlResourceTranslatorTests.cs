using System.Windows.Controls;
using NUnit.Framework;

namespace LeXtudio.Windows.Tests;

[TestFixture]
public sealed class WpfXamlResourceTranslatorTests
{
    [Test]
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

        Assert.That(specs.Select(spec => spec.Key), Is.EqualTo(new[]
        {
            "DataGridCellStyle",
            "DefaultFilter",
            "AssemblyFlagsFilter"
        }));

        var flagsTemplate = (DataGridExtensions.FilterControlTemplate)specs[2].CreateValue();
        Assert.That(flagsTemplate.Kind, Is.EqualTo(DataGridExtensions.FilterKind.Flags));
        Assert.That(flagsTemplate.FlagsType, Is.EqualTo(typeof(System.Reflection.AssemblyFlags)));
    }

    [Test]
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

        Assert.That(specs, Has.Length.EqualTo(1));
        Assert.That(specs[0].Key, Is.EqualTo("Template"));
        Assert.That(specs[0].CreateValue(), Is.EqualTo("fallback"));
    }

    [Test]
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

        Assert.That(report.TranslatedKeys, Is.EqualTo(new[] { "ItemContainerStyle" }));
        Assert.That(report.FallbackKeys, Is.EqualTo(new[] { "Template" }));
        Assert.That(report.SkippedKeys, Is.EqualTo(new[] { "Unsupported", "Template" }));
    }

    [Test]
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

        Assert.That(specs.Select(spec => spec.Key), Is.EqualTo(new[] { "BaseCellStyle", "DataGridCellStyle" }));
        var styleSpec = (StyleSpec)specs[1].Descriptor!;
        var templateSetter = styleSpec.Setters.SingleOrDefault(setter => setter.PropertyName == "Template");
        Assert.That(templateSetter?.Value, Is.TypeOf<System.Windows.Controls.ControlTemplate>());
        Assert.That(((IWpfTemplateBridge)templateSetter!.Value!).TargetType, Is.EqualTo(typeof(DataGridCell)));
    }

    [Test]
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

        Assert.That(specs.Select(spec => spec.Key), Is.EqualTo(new[] { "converter" }));
        Assert.That(specs[0].CreateValue(), Is.SameAs(SharedConverter.Instance));
        Assert.That(report.TranslatedKeys, Is.EqualTo(new[] { "converter" }));
        Assert.That(report.SkippedKeys, Is.Empty);
    }

    [Test]
    public void DataTemplateSpecExposesResourceDictionaryFactoryOverload()
    {
        var overload = typeof(WpfResourceSpec).GetMethods()
            .SingleOrDefault(method =>
                method.Name == nameof(WpfResourceSpec.DataTemplate)
                && method.GetParameters() is { Length: 2 } parameters
                && parameters[1].ParameterType.GenericTypeArguments.FirstOrDefault() == typeof(System.Windows.ResourceDictionary));

        Assert.That(overload, Is.Not.Null);
    }

    [Test]
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

        Assert.That(specs.Select(spec => spec.Key), Is.EqualTo(new[] { "TextBlob", "GridDetails" }));
        Assert.That(report.TranslatedKeys, Is.EqualTo(new[] { "TextBlob", "GridDetails" }));
        Assert.That(report.FallbackKeys, Is.Empty);
    }

    [Test]
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

        Assert.That(specs, Has.Length.EqualTo(1));
        Assert.That(specs[0].Key, Is.EqualTo(typeof(SampleRow)));
        Assert.That(report.TranslatedKeys, Is.EqualTo(new[] { typeof(SampleRow).FullName }));
    }

    [Test]
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

        Assert.That(specs.Select(spec => spec.Key), Is.EqualTo(new object[] { "nullVisConv", typeof(SampleRow) }));
        Assert.That(specs[0].CreateValue(), Is.SameAs(SharedConverter.Instance));
        Assert.That(report.TranslatedKeys, Is.EqualTo(new[] { "nullVisConv", typeof(SampleRow).FullName }));
        Assert.That(report.SkippedKeys, Is.Empty);
    }

    [Test]
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

        Assert.That(specs.Select(spec => spec.Key), Is.EqualTo(new[] { "BackgroundConverter", "alternatingWithBinding" }));
        var converterSpec = (AlternationConverterSpec)specs[0].Descriptor!;
        Assert.That(converterSpec.Values, Has.Count.EqualTo(2));
        Assert.That(converterSpec.Values[1].Opacity, Is.EqualTo(0.15));
        var styleSpec = (StyleSpec)specs[1].Descriptor!;
        Assert.That(styleSpec.TargetType, Is.EqualTo(typeof(Microsoft.UI.Xaml.Controls.ListViewItem)));
        Assert.That(styleSpec.BasedOnReference?.Key, Is.EqualTo("ListViewItem"));
        Assert.That(styleSpec.Setters.Single(setter => setter.PropertyName == "Background").Value, Is.TypeOf<System.Windows.Data.Binding>());
        var contextMenu = (ContextMenuSpec)styleSpec.Setters.Single(setter => setter.PropertyName == "ContextMenu").Value!;
        Assert.That(contextMenu.Items, Has.Count.EqualTo(2));
        Assert.That(contextMenu.Items[1].CommandParameter, Is.EqualTo("Value"));
        Assert.That(report.TranslatedKeys, Is.EqualTo(new[] { "BackgroundConverter", "alternatingWithBinding" }));
        Assert.That(report.SkippedKeys, Is.Empty);
    }

    [Test]
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

        Assert.That(specs.Select(spec => spec.Key), Is.EqualTo(new object[] { typeof(ChoiceRow) }));
        Assert.That(report.SkippedKeys, Is.Empty);

        Assert.That(report.TranslatedKeys, Is.EqualTo(new[] { typeof(ChoiceRow).FullName }));
    }

    [Test]
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

        Assert.That(specs.Select(spec => spec.Key), Is.EqualTo(new object[] { typeof(MultiChoiceRow) }));
        Assert.That(report.TranslatedKeys, Is.EqualTo(new[] { typeof(MultiChoiceRow).FullName }));
        Assert.That(report.SkippedKeys, Is.Empty);
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
