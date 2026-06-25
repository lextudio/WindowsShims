using System.Windows.Controls;
using System.Windows.Data;
using NUnit.Framework;

namespace LeXtudio.Windows.Tests;

[TestFixture]
public sealed class WpfTemplateFactoryTests
{
    [Test]
    public void BindingAssignmentAppliesBindingToTarget()
    {
        var target = new Target();
        var assignment = BindingAssignment.To(nameof(Target.Text), new Binding("Name"));

        assignment.Apply(target, new Source("metadata"));

        Assert.That(target.Text, Is.EqualTo("metadata"));
    }

    [Test]
    public void TemplateFactorySurfaceIsAvailable()
    {
        var method = typeof(WpfTemplateFactory).GetMethods()
            .SingleOrDefault(method => method.Name == nameof(WpfTemplateFactory.Create) && method.IsGenericMethodDefinition);

        Assert.That(method, Is.Not.Null);
    }

    [Test]
    public void DataGridColumnSpecDescribesTextColumn()
    {
        var spec = DataGridColumnSpec.Text("Meaning", new Binding("Meaning"));

        Assert.That(spec.Kind, Is.EqualTo(DataGridColumnKind.Text));
        Assert.That(spec.Header, Is.EqualTo("Meaning"));
        Assert.That(spec.Binding.Path?.Path, Is.EqualTo("Meaning"));
        Assert.That(spec.IsReadOnly, Is.True);
    }

    [Test]
    public void DataGridColumnSpecDescribesCheckBoxColumn()
    {
        var spec = DataGridColumnSpec.CheckBox("Value", new Binding("Value"));

        Assert.That(spec.Kind, Is.EqualTo(DataGridColumnKind.CheckBox));
        Assert.That(spec.Header, Is.EqualTo("Value"));
        Assert.That(spec.Binding.Path?.Path, Is.EqualTo("Value"));
        Assert.That(spec.IsReadOnly, Is.True);
    }

    private sealed record Source(string Name);

    private sealed class Target
    {
        public string? Text { get; set; }
    }
}
