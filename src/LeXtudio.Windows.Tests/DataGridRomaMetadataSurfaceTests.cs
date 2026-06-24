using System.Reflection;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using DataGridExtensions;
using NUnit.Framework;

namespace LeXtudio.Windows.Tests;

[TestFixture]
public sealed class DataGridRomaMetadataSurfaceTests
{
    [Test]
    public void ShimDataTemplateCarriesFactoryForRowDetails()
    {
        Assert.That(typeof(ShimDataTemplate).IsSubclassOf(typeof(Microsoft.UI.Xaml.DataTemplate)), Is.True);

        var factory = typeof(ShimDataTemplate).GetProperty(nameof(ShimDataTemplate.Factory));
        Assert.That(factory, Is.Not.Null);
        Assert.That(factory!.PropertyType, Is.EqualTo(typeof(Func<object?, Microsoft.UI.Xaml.FrameworkElement?>)));

        var ctor = typeof(ShimDataTemplate).GetConstructor([typeof(Func<object?, Microsoft.UI.Xaml.FrameworkElement?>)]);
        Assert.That(ctor, Is.Not.Null);
    }

    [Test]
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

        Assert.That(bridge, Is.Not.Null);
        Assert.That(bridge!.PropertyType, Is.EqualTo(typeof(IWpfTemplateBridge)));
        Assert.That(factory, Is.Not.Null);
        Assert.That(factory!.PropertyType, Is.EqualTo(typeof(Func<object?, Microsoft.UI.Xaml.FrameworkElement?>)));
        Assert.That(effectiveRow, Is.Not.Null);
        Assert.That(effectiveRow!.PropertyType, Is.EqualTo(typeof(DataGridRow)));
        Assert.That(setOwner, Is.Not.Null);
    }

    [Test]
    public void DataGridHelperTransfersDetailsTemplateSelector()
    {
        var transfer = typeof(DataGrid).Assembly
            .GetType("System.Windows.Controls.DataGridHelper")!
            .GetMethod("TransferProperty", BindingFlags.Static | BindingFlags.NonPublic);

        Assert.That(transfer, Is.Not.Null);
        Assert.That(typeof(DataGrid).GetProperty(nameof(DataGrid.RowDetailsTemplateSelector)), Is.Not.Null);
        Assert.That(typeof(DataGridRow).GetProperty("DetailsTemplateSelector", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public), Is.Not.Null);
    }

    [Test]
    public void RowDetailsCanBeBuiltFromTemplateSelectorOnly()
    {
        var buildRowDetails = typeof(DataGridRow).GetMethod(
            "BuildRowDetails",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var computeVisibility = typeof(DataGridRow).GetMethod(
            "ComputeDetailsVisibility",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(buildRowDetails, Is.Not.Null);
        Assert.That(computeVisibility, Is.Not.Null);
        Assert.That(
            typeof(DataGridRow).GetProperty(
                "DetailsPresenter",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public),
            Is.Not.Null);
    }

    [Test]
    public void DataGridCellStyleApplicationSurfaceExists()
    {
        var applyStyle = typeof(DataGridCell).GetMethod(
            "ApplyShimCellStyle",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var appliedStyle = typeof(DataGridCell).GetProperty(
            "ShimAppliedCellStyle",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        Assert.That(applyStyle, Is.Not.Null);
        Assert.That(appliedStyle, Is.Not.Null);
    }

    [Test]
    public void FilterStatePreservesTextSeparatelyFromFilterObject()
    {
        var stateType = typeof(DataGridFilter).GetNestedType("State", BindingFlags.NonPublic);
        Assert.That(stateType, Is.Not.Null);

        var columnFilters = stateType!.GetField("ColumnFilters", BindingFlags.Instance | BindingFlags.NonPublic);
        var columnFilterText = stateType.GetField("ColumnFilterText", BindingFlags.Instance | BindingFlags.NonPublic);
        var contentFactory = stateType.GetField("ContentFilterFactory", BindingFlags.Instance | BindingFlags.Public);

        Assert.That(columnFilters, Is.Not.Null);
        Assert.That(columnFilterText, Is.Not.Null);
        Assert.That(contentFactory, Is.Not.Null);
        Assert.That(contentFactory!.FieldType, Is.EqualTo(typeof(IContentFilterFactory)));
    }
}
