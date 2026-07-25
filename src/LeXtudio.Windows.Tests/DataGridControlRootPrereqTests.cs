using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using Xunit;
using WpfItemsControl = System.Windows.Controls.ItemsControl;

namespace LeXtudio.Windows.Tests;

public sealed class DataGridControlRootPrereqTests
{
    private sealed class AlwaysValidRule : ValidationRule
    {
        public override ValidationResult Validate(object? value, CultureInfo cultureInfo)
            => ValidationResult.ValidResult;
    }

    private sealed record Person(string Name);

    [Fact]
    public void LinkedValidationRuleRoundTrips()
    {
        var rule = new AlwaysValidRule();

        var result = rule.Validate("anything", CultureInfo.InvariantCulture);

        Assert.True(result.IsValid);
        Assert.Equal(ValidationResult.ValidResult, result);
    }

    [Fact]
    public void BindingGroupBridgeProvidesRowValidationSurface()
    {
        // Construction is dispatcher-bound (DependencyObject-derived), so the
        // bridge is verified at surface level like the other shells.
        Assert.NotNull(typeof(BindingGroup).GetProperty(nameof(BindingGroup.ValidationRules)));
        Assert.NotNull(typeof(BindingGroup).GetProperty(nameof(BindingGroup.SharesProposedValues)));
        Assert.NotNull(typeof(BindingGroup).GetMethod(nameof(BindingGroup.BeginEdit)));
        Assert.Equal(typeof(bool), typeof(BindingGroup).GetMethod(nameof(BindingGroup.CommitEdit))!.ReturnType);
        Assert.NotNull(typeof(BindingGroup).GetMethod(nameof(BindingGroup.CancelEdit)));
    }

    [Fact]
    public void PropertyGroupDescriptionExtractsGroupNames()
    {
        var description = new PropertyGroupDescription("Name");

        var name = description.GroupNameFromItem(new Person("Ada"), 0, CultureInfo.InvariantCulture);
        var fallback = new PropertyGroupDescription().GroupNameFromItem("raw", 0, CultureInfo.InvariantCulture);

        Assert.Equal("Ada", name);
        Assert.Equal("raw", fallback);
        Assert.Equal(StringComparison.Ordinal, description.StringComparison);
    }

    [Fact]
    public void HeaderShellsProvideExpectedSurface()
    {
        var header = typeof(System.Windows.Controls.Primitives.DataGridColumnHeader);
        var presenter = typeof(System.Windows.Controls.Primitives.DataGridColumnHeadersPresenter);

        Assert.NotNull(header.GetProperty("Column"));
        Assert.True(presenter.IsSubclassOf(typeof(WpfItemsControl)));
    }

    [Fact]
    public void EditableCollectionViewInterfacesAreLinked()
    {
        var view = typeof(System.ComponentModel.IEditableCollectionView);
        var addNew = typeof(System.ComponentModel.IEditableCollectionViewAddNewItem);

        Assert.NotNull(view.GetMethod("AddNew"));
        Assert.NotNull(addNew.GetMethod("AddNewItem"));
    }
}
