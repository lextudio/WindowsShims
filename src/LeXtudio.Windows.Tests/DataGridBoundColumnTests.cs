using System.Reflection;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.UI.Xaml;
using Xunit;

namespace LeXtudio.Windows.Tests;

public sealed class DataGridBoundColumnTests
{
    [Fact]
    public void DataGridCellProvidesExpectedShellSurface()
    {
        Assert.True(typeof(DataGridCell).IsSubclassOf(typeof(System.Windows.Controls.ContentControl)));
        Assert.NotNull(typeof(DataGridCell).GetProperty(nameof(DataGridCell.IsEditing)));
        Assert.NotNull(typeof(DataGridCell).GetProperty(nameof(DataGridCell.Column)));
    }

    [Fact]
    public void DataGridBoundColumnProvidesBindingAndStyleSurface()
    {
        Assert.True(typeof(DataGridBoundColumn).IsSubclassOf(typeof(DataGridColumn)));

        Assert.Equal(typeof(BindingBase), typeof(DataGridBoundColumn).GetProperty(nameof(DataGridBoundColumn.Binding))?.PropertyType);
        Assert.Equal(typeof(Style), typeof(DataGridBoundColumn).GetProperty(nameof(DataGridBoundColumn.ElementStyle))?.PropertyType);
        Assert.Equal(typeof(Style), typeof(DataGridBoundColumn).GetProperty(nameof(DataGridBoundColumn.EditingElementStyle))?.PropertyType);

        Assert.NotNull(typeof(DataGridBoundColumn).GetField(nameof(DataGridBoundColumn.ElementStyleProperty), BindingFlags.Public | BindingFlags.Static));
        Assert.NotNull(typeof(DataGridBoundColumn).GetField(nameof(DataGridBoundColumn.EditingElementStyleProperty), BindingFlags.Public | BindingFlags.Static));
    }

    [Fact]
    public void DataGridColumnProvidesSortAndVirtualRefreshSurface()
    {
        Assert.Equal(typeof(string), typeof(DataGridColumn).GetProperty(nameof(DataGridColumn.SortMemberPath))?.PropertyType);
        Assert.NotNull(typeof(DataGridColumn).GetField(nameof(DataGridColumn.SortMemberPathProperty), BindingFlags.Public | BindingFlags.Static));

        var refresh = typeof(DataGridColumn).GetMethod(
            "RefreshCellContent",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(refresh);
        Assert.True(refresh!.IsVirtual);
    }

    [Fact]
    public void BindingOperationsProvidesWpfFacade()
    {
        Assert.NotNull(typeof(BindingOperations).GetMethod(nameof(BindingOperations.SetBinding)));
        Assert.NotNull(typeof(BindingOperations).GetMethod(nameof(BindingOperations.ClearBinding)));
    }

    [Fact]
    public void DataGridTextColumnProvidesExpectedSurface()
    {
        Assert.True(typeof(DataGridTextColumn).IsSubclassOf(typeof(DataGridBoundColumn)));
        Assert.Equal(typeof(Style), typeof(DataGridTextColumn).GetProperty(nameof(DataGridTextColumn.DefaultElementStyle))?.PropertyType);
        Assert.Equal(typeof(Style), typeof(DataGridTextColumn).GetProperty(nameof(DataGridTextColumn.DefaultEditingElementStyle))?.PropertyType);

        Assert.NotNull(typeof(DataGridTextColumn).GetMethod("GenerateElement", BindingFlags.Instance | BindingFlags.NonPublic));
        Assert.NotNull(typeof(DataGridTextColumn).GetMethod("GenerateEditingElement", BindingFlags.Instance | BindingFlags.NonPublic));
        Assert.NotNull(typeof(DataGridTextColumn).GetMethod("PrepareCellForEdit", BindingFlags.Instance | BindingFlags.NonPublic));
    }

    [Fact]
    public void DataGridCheckBoxColumnProvidesExpectedSurface()
    {
        Assert.True(typeof(DataGridCheckBoxColumn).IsSubclassOf(typeof(DataGridBoundColumn)));
        Assert.Equal(typeof(Style), typeof(DataGridCheckBoxColumn).GetProperty(nameof(DataGridCheckBoxColumn.DefaultElementStyle))?.PropertyType);
        Assert.Equal(typeof(Style), typeof(DataGridCheckBoxColumn).GetProperty(nameof(DataGridCheckBoxColumn.DefaultEditingElementStyle))?.PropertyType);
        Assert.Equal(typeof(bool), typeof(DataGridCheckBoxColumn).GetProperty(nameof(DataGridCheckBoxColumn.IsThreeState))?.PropertyType);
        Assert.NotNull(typeof(DataGridCheckBoxColumn).GetField(nameof(DataGridCheckBoxColumn.IsThreeStateProperty), BindingFlags.Public | BindingFlags.Static));

        Assert.NotNull(typeof(DataGridCheckBoxColumn).GetMethod("GenerateElement", BindingFlags.Instance | BindingFlags.NonPublic));
        Assert.NotNull(typeof(DataGridCheckBoxColumn).GetMethod("GenerateEditingElement", BindingFlags.Instance | BindingFlags.NonPublic));
        Assert.NotNull(typeof(DataGridCheckBoxColumn).GetMethod("PrepareCellForEdit", BindingFlags.Instance | BindingFlags.NonPublic));
    }

    [Fact]
    public void DataGridTemplateColumnProvidesExpectedSurface()
    {
        Assert.True(typeof(DataGridTemplateColumn).IsSubclassOf(typeof(DataGridColumn)));
        Assert.Equal(typeof(DataTemplate), typeof(DataGridTemplateColumn).GetProperty(nameof(DataGridTemplateColumn.CellTemplate))?.PropertyType);
        Assert.Equal(typeof(DataTemplateSelector), typeof(DataGridTemplateColumn).GetProperty(nameof(DataGridTemplateColumn.CellTemplateSelector))?.PropertyType);
        Assert.Equal(typeof(DataTemplate), typeof(DataGridTemplateColumn).GetProperty(nameof(DataGridTemplateColumn.CellEditingTemplate))?.PropertyType);
        Assert.Equal(typeof(DataTemplateSelector), typeof(DataGridTemplateColumn).GetProperty(nameof(DataGridTemplateColumn.CellEditingTemplateSelector))?.PropertyType);

        Assert.NotNull(typeof(DataGridTemplateColumn).GetField(nameof(DataGridTemplateColumn.CellTemplateProperty), BindingFlags.Public | BindingFlags.Static));
        Assert.NotNull(typeof(DataGridTemplateColumn).GetField(nameof(DataGridTemplateColumn.CellTemplateSelectorProperty), BindingFlags.Public | BindingFlags.Static));
        Assert.NotNull(typeof(DataGridTemplateColumn).GetField(nameof(DataGridTemplateColumn.CellEditingTemplateProperty), BindingFlags.Public | BindingFlags.Static));
        Assert.NotNull(typeof(DataGridTemplateColumn).GetField(nameof(DataGridTemplateColumn.CellEditingTemplateSelectorProperty), BindingFlags.Public | BindingFlags.Static));

        Assert.NotNull(typeof(DataGridTemplateColumn).GetMethod("GenerateElement", BindingFlags.Instance | BindingFlags.NonPublic));
        Assert.NotNull(typeof(DataGridTemplateColumn).GetMethod("GenerateEditingElement", BindingFlags.Instance | BindingFlags.NonPublic));
    }

    [Fact]
    public void DataGridHyperlinkColumnProvidesExpectedSurface()
    {
        // Session 121 (gap survey item 7): replaced the former placeholder
        // (`new TextBlock()`, no binding, no click) with a real, working
        // hyperlink cell — verified live via roma.probe.metadata-hyperlink-column
        // (this reflection-surface check follows the same convention as the
        // other column tests in this file; GenerateElement needs a live UI
        // thread/DataGridCell, not exercised in this headless suite).
        Assert.True(typeof(DataGridHyperlinkColumn).IsSubclassOf(typeof(DataGridBoundColumn)));
        Assert.Equal(typeof(string), typeof(DataGridHyperlinkColumn).GetProperty(nameof(DataGridHyperlinkColumn.TargetName))?.PropertyType);
        Assert.Equal(typeof(BindingBase), typeof(DataGridHyperlinkColumn).GetProperty(nameof(DataGridHyperlinkColumn.ContentBinding))?.PropertyType);

        Assert.NotNull(typeof(DataGridHyperlinkColumn).GetMethod("GenerateElement", BindingFlags.Instance | BindingFlags.NonPublic));
        Assert.NotNull(typeof(DataGridHyperlinkColumn).GetMethod("GenerateEditingElement", BindingFlags.Instance | BindingFlags.NonPublic));
        Assert.NotNull(typeof(DataGridHyperlinkColumn).GetMethod("NavigateToBoundUri", BindingFlags.Instance | BindingFlags.NonPublic));
    }
}
