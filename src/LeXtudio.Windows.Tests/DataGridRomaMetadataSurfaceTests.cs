using System.Reflection;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using DataGridExtensions;
using Xunit;

namespace LeXtudio.Windows.Tests;

public sealed class DataGridRomaMetadataSurfaceTests
{
    [Fact]
    public void ShimDataTemplateCarriesFactoryForRowDetails()
    {
        Assert.True(typeof(ShimDataTemplate).IsSubclassOf(typeof(Microsoft.UI.Xaml.DataTemplate)));

        var factory = typeof(ShimDataTemplate).GetProperty(nameof(ShimDataTemplate.Factory));
        Assert.NotNull(factory);
        Assert.Equal(typeof(Func<object?, Microsoft.UI.Xaml.FrameworkElement?>), factory!.PropertyType);

        var ctor = typeof(ShimDataTemplate).GetConstructor([typeof(Func<object?, Microsoft.UI.Xaml.FrameworkElement?>)]);
        Assert.NotNull(ctor);
    }

    [Fact]
    public void DetailsPresenterHasShimFactoryHandoff()
    {
        var bridge = typeof(DataGridDetailsPresenter).GetProperty(
            "ShimTemplateBridge",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var factory = typeof(DataGridDetailsPresenter).GetProperty(
            "ShimContentFactory",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var effectiveRow = typeof(DataGridDetailsPresenter).GetProperty(
            "EffectiveRow",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var setOwner = typeof(DataGridDetailsPresenter).GetMethod(
            "SetShimOwnerRow",
            BindingFlags.Instance | BindingFlags.NonPublic,
            [typeof(DataGridRow)]);

        Assert.NotNull(bridge);
        Assert.Equal(typeof(IWpfTemplateBridge), bridge!.PropertyType);
        Assert.NotNull(factory);
        Assert.Equal(typeof(Func<object?, Microsoft.UI.Xaml.FrameworkElement?>), factory!.PropertyType);
        Assert.NotNull(effectiveRow);
        Assert.Equal(typeof(DataGridRow), effectiveRow!.PropertyType);
        Assert.NotNull(setOwner);
    }

    [Fact]
    public void DataGridHelperTransfersDetailsTemplateSelector()
    {
        var transfer = typeof(DataGrid).Assembly
            .GetType("System.Windows.Controls.DataGridHelper")!
            .GetMethod("TransferProperty", BindingFlags.Static | BindingFlags.NonPublic);

        Assert.NotNull(transfer);
        Assert.NotNull(typeof(DataGrid).GetProperty(nameof(DataGrid.RowDetailsTemplateSelector)));
        Assert.NotNull(typeof(DataGridRow).GetProperty("DetailsTemplateSelector", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public));
    }

    [Fact]
    public void RowDetailsCanBeBuiltFromTemplateSelectorOnly()
    {
        var buildRowDetails = typeof(DataGridRow).GetMethod(
            "BuildRowDetails",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var computeVisibility = typeof(DataGridRow).GetMethod(
            "ComputeDetailsVisibility",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(buildRowDetails);
        Assert.NotNull(computeVisibility);
        Assert.NotNull(typeof(DataGridRow).GetProperty( "DetailsPresenter", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public));
    }

    [Fact]
    public void DataGridCellStyleApplicationSurfaceExists()
    {
        var applyStyle = typeof(DataGridCell).GetMethod(
            "ApplyShimCellStyle",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var appliedStyle = typeof(DataGridCell).GetProperty(
            "ShimAppliedCellStyle",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        Assert.NotNull(applyStyle);
        Assert.NotNull(appliedStyle);
    }

    [Fact]
    public void FilterStatePreservesTextSeparatelyFromFilterObject()
    {
        var stateType = typeof(DataGridFilter).GetNestedType("State", BindingFlags.NonPublic);
        Assert.NotNull(stateType);

        var columnFilters = stateType!.GetField("ColumnFilters", BindingFlags.Instance | BindingFlags.NonPublic);
        var columnFilterText = stateType.GetField("ColumnFilterText", BindingFlags.Instance | BindingFlags.NonPublic);
        var contentFactory = stateType.GetField("ContentFilterFactory", BindingFlags.Instance | BindingFlags.Public);

        Assert.NotNull(columnFilters);
        Assert.NotNull(columnFilterText);
        Assert.NotNull(contentFactory);
        Assert.Equal(typeof(IContentFilterFactory), contentFactory!.FieldType);
    }
}
