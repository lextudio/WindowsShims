using System.Collections;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.UI.Xaml;
using NUnit.Framework;

namespace LeXtudio.Windows.Tests;

[TestFixture]
public sealed class DataGridComboBoxColumnTests
{
    [Test]
    public void ComboBoxColumnDerivesFromDataGridColumn()
    {
        Assert.That(typeof(DataGridComboBoxColumn).IsSubclassOf(typeof(DataGridColumn)), Is.True);
        Assert.That(typeof(DataGridComboBoxColumn).IsSubclassOf(typeof(DataGridBoundColumn)), Is.False);
    }

    [Test]
    public void ComboBoxColumnProvidesBindingSurface()
    {
        Assert.That(typeof(DataGridComboBoxColumn).GetProperty(nameof(DataGridComboBoxColumn.SelectedItemBinding))?.PropertyType, Is.EqualTo(typeof(BindingBase)));
        Assert.That(typeof(DataGridComboBoxColumn).GetProperty(nameof(DataGridComboBoxColumn.SelectedValueBinding))?.PropertyType, Is.EqualTo(typeof(BindingBase)));
        Assert.That(typeof(DataGridComboBoxColumn).GetProperty(nameof(DataGridComboBoxColumn.TextBinding))?.PropertyType, Is.EqualTo(typeof(BindingBase)));

        Assert.That(typeof(DataGridComboBoxColumn).GetProperty(nameof(DataGridComboBoxColumn.SelectedItemBinding))!.GetMethod!.IsVirtual, Is.True);
        Assert.That(typeof(DataGridComboBoxColumn).GetProperty(nameof(DataGridComboBoxColumn.SelectedValueBinding))!.GetMethod!.IsVirtual, Is.True);
        Assert.That(typeof(DataGridComboBoxColumn).GetProperty(nameof(DataGridComboBoxColumn.TextBinding))!.GetMethod!.IsVirtual, Is.True);
    }

    [Test]
    public void ComboBoxColumnProvidesItemsSourceSurface()
    {
        Assert.That(typeof(DataGridComboBoxColumn).GetProperty(nameof(DataGridComboBoxColumn.ItemsSource))?.PropertyType, Is.EqualTo(typeof(IEnumerable)));
        Assert.That(typeof(DataGridComboBoxColumn).GetProperty(nameof(DataGridComboBoxColumn.DisplayMemberPath))?.PropertyType, Is.EqualTo(typeof(string)));
        Assert.That(typeof(DataGridComboBoxColumn).GetProperty(nameof(DataGridComboBoxColumn.SelectedValuePath))?.PropertyType, Is.EqualTo(typeof(string)));

        Assert.That(typeof(DataGridComboBoxColumn).GetField(nameof(DataGridComboBoxColumn.ItemsSourceProperty), BindingFlags.Public | BindingFlags.Static), Is.Not.Null);
        Assert.That(typeof(DataGridComboBoxColumn).GetField(nameof(DataGridComboBoxColumn.DisplayMemberPathProperty), BindingFlags.Public | BindingFlags.Static), Is.Not.Null);
        Assert.That(typeof(DataGridComboBoxColumn).GetField(nameof(DataGridComboBoxColumn.SelectedValuePathProperty), BindingFlags.Public | BindingFlags.Static), Is.Not.Null);
    }

    [Test]
    public void ComboBoxColumnProvidesStyleSurface()
    {
        Assert.That(typeof(DataGridComboBoxColumn).GetProperty(nameof(DataGridComboBoxColumn.ElementStyle))?.PropertyType, Is.EqualTo(typeof(Style)));
        Assert.That(typeof(DataGridComboBoxColumn).GetProperty(nameof(DataGridComboBoxColumn.EditingElementStyle))?.PropertyType, Is.EqualTo(typeof(Style)));
        Assert.That(typeof(DataGridComboBoxColumn).GetProperty(nameof(DataGridComboBoxColumn.DefaultElementStyle))?.PropertyType, Is.EqualTo(typeof(Style)));
        Assert.That(typeof(DataGridComboBoxColumn).GetProperty(nameof(DataGridComboBoxColumn.DefaultEditingElementStyle))?.PropertyType, Is.EqualTo(typeof(Style)));

        Assert.That(typeof(DataGridComboBoxColumn).GetField(nameof(DataGridComboBoxColumn.ElementStyleProperty), BindingFlags.Public | BindingFlags.Static), Is.Not.Null);
        Assert.That(typeof(DataGridComboBoxColumn).GetField(nameof(DataGridComboBoxColumn.EditingElementStyleProperty), BindingFlags.Public | BindingFlags.Static), Is.Not.Null);
    }

    [Test]
    public void ComboBoxColumnProvidesGenerationAndEditSurface()
    {
        Assert.That(
            typeof(DataGridComboBoxColumn).GetMethod("GenerateElement", BindingFlags.Instance | BindingFlags.NonPublic),
            Is.Not.Null);
        Assert.That(
            typeof(DataGridComboBoxColumn).GetMethod("GenerateEditingElement", BindingFlags.Instance | BindingFlags.NonPublic),
            Is.Not.Null);
        Assert.That(
            typeof(DataGridComboBoxColumn).GetMethod("PrepareCellForEdit", BindingFlags.Instance | BindingFlags.NonPublic),
            Is.Not.Null);
        Assert.That(
            typeof(DataGridComboBoxColumn).GetMethod("RefreshCellContent", BindingFlags.Instance | BindingFlags.NonPublic),
            Is.Not.Null);
    }
}
