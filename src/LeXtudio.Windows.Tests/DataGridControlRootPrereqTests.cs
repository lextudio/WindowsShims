using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using NUnit.Framework;
using WpfItemsControl = System.Windows.Controls.ItemsControl;

namespace LeXtudio.Windows.Tests;

[TestFixture]
public sealed class DataGridControlRootPrereqTests
{
    private sealed class AlwaysValidRule : ValidationRule
    {
        public override ValidationResult Validate(object? value, CultureInfo cultureInfo)
            => ValidationResult.ValidResult;
    }

    private sealed record Person(string Name);

    [Test]
    public void LinkedValidationRuleRoundTrips()
    {
        var rule = new AlwaysValidRule();

        var result = rule.Validate("anything", CultureInfo.InvariantCulture);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result, Is.EqualTo(ValidationResult.ValidResult));
    }

    [Test]
    public void BindingGroupBridgeProvidesRowValidationSurface()
    {
        // Construction is dispatcher-bound (DependencyObject-derived), so the
        // bridge is verified at surface level like the other shells.
        Assert.That(typeof(BindingGroup).GetProperty(nameof(BindingGroup.ValidationRules)), Is.Not.Null);
        Assert.That(typeof(BindingGroup).GetProperty(nameof(BindingGroup.SharesProposedValues)), Is.Not.Null);
        Assert.That(typeof(BindingGroup).GetMethod(nameof(BindingGroup.BeginEdit)), Is.Not.Null);
        Assert.That(typeof(BindingGroup).GetMethod(nameof(BindingGroup.CommitEdit))!.ReturnType, Is.EqualTo(typeof(bool)));
        Assert.That(typeof(BindingGroup).GetMethod(nameof(BindingGroup.CancelEdit)), Is.Not.Null);
    }

    [Test]
    public void PropertyGroupDescriptionExtractsGroupNames()
    {
        var description = new PropertyGroupDescription("Name");

        var name = description.GroupNameFromItem(new Person("Ada"), 0, CultureInfo.InvariantCulture);
        var fallback = new PropertyGroupDescription().GroupNameFromItem("raw", 0, CultureInfo.InvariantCulture);

        Assert.That(name, Is.EqualTo("Ada"));
        Assert.That(fallback, Is.EqualTo("raw"));
        Assert.That(description.StringComparison, Is.EqualTo(StringComparison.Ordinal));
    }

    [Test]
    public void HeaderShellsProvideExpectedSurface()
    {
        var header = typeof(System.Windows.Controls.Primitives.DataGridColumnHeader);
        var presenter = typeof(System.Windows.Controls.Primitives.DataGridColumnHeadersPresenter);

        Assert.That(header.GetProperty("Column"), Is.Not.Null);
        Assert.That(presenter.IsSubclassOf(typeof(WpfItemsControl)), Is.True);
    }

    [Test]
    public void EditableCollectionViewInterfacesAreLinked()
    {
        var view = typeof(System.ComponentModel.IEditableCollectionView);
        var addNew = typeof(System.ComponentModel.IEditableCollectionViewAddNewItem);

        Assert.That(view.GetMethod("AddNew"), Is.Not.Null);
        Assert.That(addNew.GetMethod("AddNewItem"), Is.Not.Null);
    }
}
