namespace Stl.Tests.Collections;

public class OptionSetTest
{
    [Fact]
    public void BasicTest()
    {
        var options = new OptionSet();
        options = options.PassThroughAllSerializers();
        options.Items.Count.Should().Be(0);

        options.Set("A");
        options = options.PassThroughAllSerializers();
        options.Get<string>().Should().Be("A");
        options.GetRequiredService<string>().Should().Be("A");
        options.Items.Count.Should().Be(1);

        options.Set("B");
        options = options.PassThroughAllSerializers();
        options.Get<string>().Should().Be("B");
        options.GetRequiredService<string>().Should().Be("B");
        options.Items.Count.Should().Be(1);

        options.Remove<string>();
        options = options.PassThroughAllSerializers();
        options.GetOrDefault<string>("").Should().Be("");
        options.Get<string>().Should().BeNull();
        options.GetService<string>().Should().Be(null);
        options.Items.Count.Should().Be(0);

        options.Set("C");
        options.Clear();
        options.Items.Count.Should().Be(0);
    }
}
