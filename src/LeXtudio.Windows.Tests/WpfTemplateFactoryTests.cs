using System.Windows.Controls;
using System.Windows.Data;
using Xunit;

namespace LeXtudio.Windows.Tests;

public sealed class WpfTemplateFactoryTests
{
    [Fact]
    public void BindingAssignmentAppliesBindingToTarget()
    {
        var target = new Target();
        var assignment = BindingAssignment.To(nameof(Target.Text), new Binding("Name"));

        assignment.Apply(target, new Source("metadata"));

        Assert.Equal("metadata", target.Text);
    }

    [Fact]
    public void TemplateFactorySurfaceIsAvailable()
    {
        var method = typeof(WpfTemplateFactory).GetMethods()
            .SingleOrDefault(method => method.Name == nameof(WpfTemplateFactory.Create) && method.IsGenericMethodDefinition);

        Assert.NotNull(method);
    }

    [Fact]
    public void DataGridColumnSpecDescribesTextColumn()
    {
        var spec = DataGridColumnSpec.Text("Meaning", new Binding("Meaning"));

        Assert.Equal(DataGridColumnKind.Text, spec.Kind);
        Assert.Equal("Meaning", spec.Header);
        Assert.Equal("Meaning", spec.Binding.Path?.Path);
        Assert.True(spec.IsReadOnly);
    }

    [Fact]
    public void DataGridColumnSpecDescribesCheckBoxColumn()
    {
        var spec = DataGridColumnSpec.CheckBox("Value", new Binding("Value"));

        Assert.Equal(DataGridColumnKind.CheckBox, spec.Kind);
        Assert.Equal("Value", spec.Header);
        Assert.Equal("Value", spec.Binding.Path?.Path);
        Assert.True(spec.IsReadOnly);
    }

    private sealed record Source(string Name);

    private sealed class Target
    {
        public string? Text { get; set; }
    }
}
