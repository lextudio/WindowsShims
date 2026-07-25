using System.Collections;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.UI.Xaml;
using Xunit;

namespace LeXtudio.Windows.Tests;

public sealed class DataGridComboBoxColumnTests
{
    [Fact]
    public void ComboBoxColumnDerivesFromDataGridColumn()
    {
        Assert.True(typeof(DataGridComboBoxColumn).IsSubclassOf(typeof(DataGridColumn)));
        Assert.False(typeof(DataGridComboBoxColumn).IsSubclassOf(typeof(DataGridBoundColumn)));
    }

    [Fact]
    public void ComboBoxColumnProvidesBindingSurface()
    {
        Assert.Equal(typeof(BindingBase), typeof(DataGridComboBoxColumn).GetProperty(nameof(DataGridComboBoxColumn.SelectedItemBinding))?.PropertyType);
        Assert.Equal(typeof(BindingBase), typeof(DataGridComboBoxColumn).GetProperty(nameof(DataGridComboBoxColumn.SelectedValueBinding))?.PropertyType);
        Assert.Equal(typeof(BindingBase), typeof(DataGridComboBoxColumn).GetProperty(nameof(DataGridComboBoxColumn.TextBinding))?.PropertyType);

        Assert.True(typeof(DataGridComboBoxColumn).GetProperty(nameof(DataGridComboBoxColumn.SelectedItemBinding))!.GetMethod!.IsVirtual);
        Assert.True(typeof(DataGridComboBoxColumn).GetProperty(nameof(DataGridComboBoxColumn.SelectedValueBinding))!.GetMethod!.IsVirtual);
        Assert.True(typeof(DataGridComboBoxColumn).GetProperty(nameof(DataGridComboBoxColumn.TextBinding))!.GetMethod!.IsVirtual);
    }

    [Fact]
    public void ComboBoxColumnProvidesItemsSourceSurface()
    {
        Assert.Equal(typeof(IEnumerable), typeof(DataGridComboBoxColumn).GetProperty(nameof(DataGridComboBoxColumn.ItemsSource))?.PropertyType);
        Assert.Equal(typeof(string), typeof(DataGridComboBoxColumn).GetProperty(nameof(DataGridComboBoxColumn.DisplayMemberPath))?.PropertyType);
        Assert.Equal(typeof(string), typeof(DataGridComboBoxColumn).GetProperty(nameof(DataGridComboBoxColumn.SelectedValuePath))?.PropertyType);

        Assert.NotNull(typeof(DataGridComboBoxColumn).GetField(nameof(DataGridComboBoxColumn.ItemsSourceProperty), BindingFlags.Public | BindingFlags.Static));
        Assert.NotNull(typeof(DataGridComboBoxColumn).GetField(nameof(DataGridComboBoxColumn.DisplayMemberPathProperty), BindingFlags.Public | BindingFlags.Static));
        Assert.NotNull(typeof(DataGridComboBoxColumn).GetField(nameof(DataGridComboBoxColumn.SelectedValuePathProperty), BindingFlags.Public | BindingFlags.Static));
    }

    [Fact]
    public void ComboBoxColumnProvidesStyleSurface()
    {
        Assert.Equal(typeof(Style), typeof(DataGridComboBoxColumn).GetProperty(nameof(DataGridComboBoxColumn.ElementStyle))?.PropertyType);
        Assert.Equal(typeof(Style), typeof(DataGridComboBoxColumn).GetProperty(nameof(DataGridComboBoxColumn.EditingElementStyle))?.PropertyType);
        Assert.Equal(typeof(Style), typeof(DataGridComboBoxColumn).GetProperty(nameof(DataGridComboBoxColumn.DefaultElementStyle))?.PropertyType);
        Assert.Equal(typeof(Style), typeof(DataGridComboBoxColumn).GetProperty(nameof(DataGridComboBoxColumn.DefaultEditingElementStyle))?.PropertyType);

        Assert.NotNull(typeof(DataGridComboBoxColumn).GetField(nameof(DataGridComboBoxColumn.ElementStyleProperty), BindingFlags.Public | BindingFlags.Static));
        Assert.NotNull(typeof(DataGridComboBoxColumn).GetField(nameof(DataGridComboBoxColumn.EditingElementStyleProperty), BindingFlags.Public | BindingFlags.Static));
    }

    [Fact]
    public void ComboBoxColumnProvidesGenerationAndEditSurface()
    {
        Assert.NotNull(typeof(DataGridComboBoxColumn).GetMethod("GenerateElement", BindingFlags.Instance | BindingFlags.NonPublic));
        Assert.NotNull(typeof(DataGridComboBoxColumn).GetMethod("GenerateEditingElement", BindingFlags.Instance | BindingFlags.NonPublic));
        Assert.NotNull(typeof(DataGridComboBoxColumn).GetMethod("PrepareCellForEdit", BindingFlags.Instance | BindingFlags.NonPublic));
        Assert.NotNull(typeof(DataGridComboBoxColumn).GetMethod("RefreshCellContent", BindingFlags.Instance | BindingFlags.NonPublic));
    }
}
