using Microsoft.Extensions.DependencyInjection;

namespace Stl.Tests.Collections;

public class ImmutableOptionSetTest
{
    [Fact]
    public void BasicTest()
    {
        var options = new ImmutableOptionSet();
        options = options.PassThroughAllSerializers();
        options.Items.Count.Should().Be(0);

        options = options.Set("A");
        options = options.PassThroughAllSerializers();
        options.Get<string>().Should().Be("A");
        options.GetRequiredService<string>().Should().Be("A");
        options.Items.Count.Should().Be(1);

        options = options.Set("B");
        options = options.PassThroughAllSerializers();
        options.Get<string>().Should().Be("B");
        options.GetRequiredService<string>().Should().Be("B");
        options.Items.Count.Should().Be(1);

        options = options.Remove<string>();
        options = options.PassThroughAllSerializers();
        options.TryGet<string>().Should().Be(null);
        Assert.Throws<KeyNotFoundException>(() => {
            options.Get<string>();
        });
        options.GetService<string>().Should().Be(null);
        options.Items.Count.Should().Be(0);

        options = options.Set("C");
        options = ImmutableOptionSet.Empty;
        options.Items.Count.Should().Be(0);
    }
}
