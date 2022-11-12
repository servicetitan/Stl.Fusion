namespace Stl.Tests.Collections;

public class ImmutableOptionSetTest
{
    [Fact]
    public void StringTest()
    {
        var options = new ImmutableOptionSet();
        options = options.PassThroughAllSerializers();
        options.Items.Count.Should().Be(0);

        options = options.Set("A");
        options = options.PassThroughAllSerializers();
        options.Get<string>().Should().Be("A");
        options.Get("").Should().Be("A");
        options.GetRequiredService<string>().Should().Be("A");
        options.Items.Count.Should().Be(1);

        options = options.Set("B");
        options = options.PassThroughAllSerializers();
        options.Get<string>().Should().Be("B");
        options.Get("").Should().Be("B");
        options.GetRequiredService<string>().Should().Be("B");
        options.Items.Count.Should().Be(1);

        options = options.Remove<string>();
        options = options.PassThroughAllSerializers();
        options.Get<string>().Should().BeNull();
        options.Get("").Should().Be("");
        options.GetService<string>().Should().Be(null);
        options.Items.Count.Should().Be(0);
    }

    [Fact]
    public void StructTest()
    {
        var options = new ImmutableOptionSet();
        options = options.PassThroughAllSerializers();
        options.Items.Count.Should().Be(0);

        options = options.Set(1L);
        options = options.PassThroughAllSerializers();
        options.Get<long>().Should().Be(1L);
        options.Get(-1L).Should().Be(1L);
        options.GetRequiredService<long>().Should().Be(1L);
        options.Items.Count.Should().Be(1);

        options = options.Set(2L);
        options = options.PassThroughAllSerializers();
        options.Get<long>().Should().Be(2L);
        options.Get(-1L).Should().Be(2L);
        options.GetRequiredService<long>().Should().Be(2L);
        options.Items.Count.Should().Be(1);

        options = options.Remove<long>();
        options = options.PassThroughAllSerializers();
        options.Get<long>().Should().Be(0L);
        options.Get(-1L).Should().Be(-1L);
        options.Items.Count.Should().Be(0);
    }

    [Fact]
    public void SetManyTest()
    {
        var options = new ImmutableOptionSet().Set(1L).Set("A");
        var copy = new ImmutableOptionSet().SetMany(options);

        copy.Items.Count.Should().Be(2);
        copy.Get<long>().Should().Be(1L);
        copy.Get<string>().Should().Be("A");
    }

    [Fact]
    public void ReplaceTest()
    {
        var options = new ImmutableOptionSet();
        options = options.Replace(null, "A");
        options.Get<string>().Should().Be("A");
        options = options.Replace("A", "B");
        options.Get<string>().Should().Be("B");

        options = options.Replace("C", "D");
        options.Get<string>().Should().Be("B");
    }
}
