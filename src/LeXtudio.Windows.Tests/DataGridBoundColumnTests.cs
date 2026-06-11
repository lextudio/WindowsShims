using System.Reflection;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.UI.Xaml;
using NUnit.Framework;

namespace LeXtudio.Windows.Tests;

[TestFixture]
public sealed class DataGridBoundColumnTests
{
    [Test]
    public void DataGridCellProvidesExpectedShellSurface()
    {
        Assert.That(typeof(DataGridCell).IsSubclassOf(typeof(ContentControl)), Is.True);
        Assert.That(typeof(DataGridCell).GetProperty(nameof(DataGridCell.IsEditing)), Is.Not.Null);
        Assert.That(typeof(DataGridCell).GetProperty(nameof(DataGridCell.Column)), Is.Not.Null);
    }

    [Test]
    public void DataGridBoundColumnProvidesBindingAndStyleSurface()
    {
        Assert.That(typeof(DataGridBoundColumn).IsSubclassOf(typeof(DataGridColumn)), Is.True);

        Assert.That(typeof(DataGridBoundColumn).GetProperty(nameof(DataGridBoundColumn.Binding))?.PropertyType, Is.EqualTo(typeof(BindingBase)));
        Assert.That(typeof(DataGridBoundColumn).GetProperty(nameof(DataGridBoundColumn.ElementStyle))?.PropertyType, Is.EqualTo(typeof(Style)));
        Assert.That(typeof(DataGridBoundColumn).GetProperty(nameof(DataGridBoundColumn.EditingElementStyle))?.PropertyType, Is.EqualTo(typeof(Style)));

        Assert.That(typeof(DataGridBoundColumn).GetField(nameof(DataGridBoundColumn.ElementStyleProperty), BindingFlags.Public | BindingFlags.Static), Is.Not.Null);
        Assert.That(typeof(DataGridBoundColumn).GetField(nameof(DataGridBoundColumn.EditingElementStyleProperty), BindingFlags.Public | BindingFlags.Static), Is.Not.Null);
    }

    [Test]
    public void DataGridColumnProvidesSortAndVirtualRefreshSurface()
    {
        Assert.That(typeof(DataGridColumn).GetProperty(nameof(DataGridColumn.SortMemberPath))?.PropertyType, Is.EqualTo(typeof(string)));
        Assert.That(typeof(DataGridColumn).GetField(nameof(DataGridColumn.SortMemberPathProperty), BindingFlags.Public | BindingFlags.Static), Is.Not.Null);

        var refresh = typeof(DataGridColumn).GetMethod(
            "RefreshCellContent",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(refresh, Is.Not.Null);
        Assert.That(refresh!.IsVirtual, Is.True);
    }

    [Test]
    public void BindingOperationsProvidesWpfFacade()
    {
        Assert.That(typeof(BindingOperations).GetMethod(nameof(BindingOperations.SetBinding)), Is.Not.Null);
        Assert.That(typeof(BindingOperations).GetMethod(nameof(BindingOperations.ClearBinding)), Is.Not.Null);
    }

    [Test]
    public void DataGridTextColumnProvidesExpectedSurface()
    {
        Assert.That(typeof(DataGridTextColumn).IsSubclassOf(typeof(DataGridBoundColumn)), Is.True);
        Assert.That(typeof(DataGridTextColumn).GetProperty(nameof(DataGridTextColumn.DefaultElementStyle))?.PropertyType, Is.EqualTo(typeof(Style)));
        Assert.That(typeof(DataGridTextColumn).GetProperty(nameof(DataGridTextColumn.DefaultEditingElementStyle))?.PropertyType, Is.EqualTo(typeof(Style)));

        Assert.That(
            typeof(DataGridTextColumn).GetMethod("GenerateElement", BindingFlags.Instance | BindingFlags.NonPublic),
            Is.Not.Null);
        Assert.That(
            typeof(DataGridTextColumn).GetMethod("GenerateEditingElement", BindingFlags.Instance | BindingFlags.NonPublic),
            Is.Not.Null);
        Assert.That(
            typeof(DataGridTextColumn).GetMethod("PrepareCellForEdit", BindingFlags.Instance | BindingFlags.NonPublic),
            Is.Not.Null);
    }

    [Test]
    public void DataGridCheckBoxColumnProvidesExpectedSurface()
    {
        Assert.That(typeof(DataGridCheckBoxColumn).IsSubclassOf(typeof(DataGridBoundColumn)), Is.True);
        Assert.That(typeof(DataGridCheckBoxColumn).GetProperty(nameof(DataGridCheckBoxColumn.DefaultElementStyle))?.PropertyType, Is.EqualTo(typeof(Style)));
        Assert.That(typeof(DataGridCheckBoxColumn).GetProperty(nameof(DataGridCheckBoxColumn.DefaultEditingElementStyle))?.PropertyType, Is.EqualTo(typeof(Style)));
        Assert.That(typeof(DataGridCheckBoxColumn).GetProperty(nameof(DataGridCheckBoxColumn.IsThreeState))?.PropertyType, Is.EqualTo(typeof(bool)));
        Assert.That(typeof(DataGridCheckBoxColumn).GetField(nameof(DataGridCheckBoxColumn.IsThreeStateProperty), BindingFlags.Public | BindingFlags.Static), Is.Not.Null);

        Assert.That(
            typeof(DataGridCheckBoxColumn).GetMethod("GenerateElement", BindingFlags.Instance | BindingFlags.NonPublic),
            Is.Not.Null);
        Assert.That(
            typeof(DataGridCheckBoxColumn).GetMethod("GenerateEditingElement", BindingFlags.Instance | BindingFlags.NonPublic),
            Is.Not.Null);
        Assert.That(
            typeof(DataGridCheckBoxColumn).GetMethod("PrepareCellForEdit", BindingFlags.Instance | BindingFlags.NonPublic),
            Is.Not.Null);
    }

    [Test]
    public void DataGridTemplateColumnProvidesExpectedSurface()
    {
        Assert.That(typeof(DataGridTemplateColumn).IsSubclassOf(typeof(DataGridColumn)), Is.True);
        Assert.That(typeof(DataGridTemplateColumn).GetProperty(nameof(DataGridTemplateColumn.CellTemplate))?.PropertyType, Is.EqualTo(typeof(DataTemplate)));
        Assert.That(typeof(DataGridTemplateColumn).GetProperty(nameof(DataGridTemplateColumn.CellTemplateSelector))?.PropertyType, Is.EqualTo(typeof(DataTemplateSelector)));
        Assert.That(typeof(DataGridTemplateColumn).GetProperty(nameof(DataGridTemplateColumn.CellEditingTemplate))?.PropertyType, Is.EqualTo(typeof(DataTemplate)));
        Assert.That(typeof(DataGridTemplateColumn).GetProperty(nameof(DataGridTemplateColumn.CellEditingTemplateSelector))?.PropertyType, Is.EqualTo(typeof(DataTemplateSelector)));

        Assert.That(typeof(DataGridTemplateColumn).GetField(nameof(DataGridTemplateColumn.CellTemplateProperty), BindingFlags.Public | BindingFlags.Static), Is.Not.Null);
        Assert.That(typeof(DataGridTemplateColumn).GetField(nameof(DataGridTemplateColumn.CellTemplateSelectorProperty), BindingFlags.Public | BindingFlags.Static), Is.Not.Null);
        Assert.That(typeof(DataGridTemplateColumn).GetField(nameof(DataGridTemplateColumn.CellEditingTemplateProperty), BindingFlags.Public | BindingFlags.Static), Is.Not.Null);
        Assert.That(typeof(DataGridTemplateColumn).GetField(nameof(DataGridTemplateColumn.CellEditingTemplateSelectorProperty), BindingFlags.Public | BindingFlags.Static), Is.Not.Null);

        Assert.That(
            typeof(DataGridTemplateColumn).GetMethod("GenerateElement", BindingFlags.Instance | BindingFlags.NonPublic),
            Is.Not.Null);
        Assert.That(
            typeof(DataGridTemplateColumn).GetMethod("GenerateEditingElement", BindingFlags.Instance | BindingFlags.NonPublic),
            Is.Not.Null);
    }
}
