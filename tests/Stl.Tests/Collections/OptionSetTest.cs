namespace Stl.Tests.Collections;

public class OptionSetTest
{
    [Fact]
    public void StringTest()
    {
        var options = new OptionSet();
        options = options.PassThroughAllSerializers();
        options.Items.Count.Should().Be(0);

        options.Set("A");
        options = options.PassThroughAllSerializers();
        options.Get<string>().Should().Be("A");
        options.GetOrDefault("").Should().Be("A");
        options.GetRequiredService<string>().Should().Be("A");
        options.Items.Count.Should().Be(1);

        options.Set("B");
        options = options.PassThroughAllSerializers();
        options.Get<string>().Should().Be("B");
        options.GetOrDefault("").Should().Be("B");
        options.GetRequiredService<string>().Should().Be("B");
        options.Items.Count.Should().Be(1);

        options.Remove<string>();
        options = options.PassThroughAllSerializers();
        options.Get<string>().Should().BeNull();
        options.GetOrDefault("").Should().Be("");
        options.GetService<string?>().Should().Be(null);
        options.Items.Count.Should().Be(0);

        options.Set("C");
        options.Clear();
        options.Items.Count.Should().Be(0);
    }

    [Fact]
    public void StructTest()
    {
        var options = new OptionSet();
        options = options.PassThroughAllSerializers();
        options.Items.Count.Should().Be(0);

        // We use Int64 type here b/c JSON serializer
        // deserializes integers to this type.

        options.Set(1L);
        options = options.PassThroughAllSerializers();
        options.GetOrDefault<long>().Should().Be(1L);
        options.GetOrDefault(-1L).Should().Be(1L);
        options.GetRequiredService<long>().Should().Be(1L);
        options.Items.Count.Should().Be(1);

        options.Set(2L);
        options = options.PassThroughAllSerializers();
        options.GetOrDefault<long>().Should().Be(2L);
        options.GetOrDefault(-1L).Should().Be(2L);
        options.GetRequiredService<long>().Should().Be(2L);
        options.Items.Count.Should().Be(1);

        options.Remove<long>();
        options = options.PassThroughAllSerializers();
        options.GetOrDefault<long>().Should().Be(0L);
        options.GetOrDefault(-1L).Should().Be(-1L);
        options.Items.Count.Should().Be(0);

        options.Set(3L);
        options.Clear();
        options.Items.Count.Should().Be(0);
    }

    [Fact]
    public void SetManyTest()
    {
        var options = new OptionSet();
        options.Set(1L);
        options.Set("A");
        var copy = new OptionSet();
        copy.SetMany(options);

        copy.Items.Count.Should().Be(2);
        copy.GetOrDefault<long>().Should().Be(1L);
        copy.Get<string>().Should().Be("A");
    }

    [Fact]
    public void ReplaceTest()
    {
        var options = new OptionSet();
        options.Replace(null, "A").Should().BeTrue();
        options.Get<string>().Should().Be("A");
        options.Replace("A", "B").Should().BeTrue();
        options.Get<string>().Should().Be("B");

        options.Replace("C", "D").Should().BeFalse();
        options.Get<string>().Should().Be("B");
    }
}
