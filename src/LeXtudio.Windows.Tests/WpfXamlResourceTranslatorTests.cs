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
                </Style>
            </ResourceDictionary>
            """;

        var specs = WpfXamlResourceTranslator.TranslateResourceDictionary(xaml, ResolveType);

        Assert.That(specs.Select(spec => spec.Key), Is.EqualTo(new[] { "BaseCellStyle", "DataGridCellStyle" }));
    }

    [Test]
    public void TranslateResourceDictionaryReadsSimpleTextBoxDataTemplate()
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
            out var report,
            WpfResourceSpec.Value("GridDetails", "fallback"));

        Assert.That(specs.Select(spec => spec.Key), Is.EqualTo(new[] { "TextBlob", "GridDetails" }));
        Assert.That(report.TranslatedKeys, Is.EqualTo(new[] { "TextBlob" }));
        Assert.That(report.FallbackKeys, Is.EqualTo(new[] { "GridDetails" }));
    }

    private static Type? ResolveType(string name)
        => name switch
        {
            "DataGridCell" => typeof(DataGridCell),
            "ListViewItem" => typeof(Microsoft.UI.Xaml.Controls.ListViewItem),
            "srm:AssemblyFlags" => typeof(System.Reflection.AssemblyFlags),
            _ => null
        };
}
