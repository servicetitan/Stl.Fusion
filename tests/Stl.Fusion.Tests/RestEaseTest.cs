using Stl.Fusion.Tests.Services;

namespace Stl.Fusion.Tests;

// checks different request patterns with RestEase
public class RestEaseTest : FusionTestBase
{
    public RestEaseTest(ITestOutputHelper @out) : base(@out) { }
    
    [Fact]
    public async Task GetFromQueryImplicit()
    {
        await using var serving = await WebHost.Serve();
        var service = ClientServices.GetRequiredService<IRestEaseClient>();

        (await service.GetFromQueryImplicit("abcD")).Should().Be("abcD");
    }
    
    [Fact]
    public async Task GetFromQuery()
    {
        await using var serving = await WebHost.Serve();
        var service = ClientServices.GetRequiredService<IRestEaseClient>();

        (await service.GetFromQuery("abcD")).Should().Be("abcD");
    }
    
    [Fact]
    public async Task GetJsonString()
    {
        await using var serving = await WebHost.Serve();
        var service = ClientServices.GetRequiredService<IRestEaseClient>();

        (await service.GetJsonString("abcD")).Value.Should().Be("abcD");
    }
    
    [Fact]
    public async Task GetFromPath()
    {
        await using var serving = await WebHost.Serve();
        var service = ClientServices.GetRequiredService<IRestEaseClient>();

        (await service.GetFromPath("abcD")).Should().Be("abcD");
    }
    
    [Fact]
    public async Task PostFromQueryImplicit()
    {
        await using var serving = await WebHost.Serve();
        var service = ClientServices.GetRequiredService<IRestEaseClient>();

        var jsonString = (await service.PostFromQueryImplicit("abcD"));
        jsonString.Value.Should().Be("abcD");
    }
    
    [Fact]
    public async Task PostFromQuery()
    {
        await using var serving = await WebHost.Serve();
        var service = ClientServices.GetRequiredService<IRestEaseClient>();

        var jsonString = (await service.PostFromQuery("abcD"));
        jsonString.Value.Should().Be("abcD");
    }
    
    [Fact]
    public async Task PostFromPath()
    {
        await using var serving = await WebHost.Serve();
        var service = ClientServices.GetRequiredService<IRestEaseClient>();

        var jsonString = (await service.PostFromPath("abcD"));
        jsonString.Value.Should().Be("abcD");
    }
    
    [Fact]
    public async Task PostWithBody()
    {
        await using var serving = await WebHost.Serve();
        var service = ClientServices.GetRequiredService<IRestEaseClient>();

        var jsonString = await service.PostWithBody(new StringWrapper("abcD"));
        jsonString.Value.Should().Be("abcD");
    }
    
    [Fact]
    public async Task ConcatQueryAndPath()
    {
        await using var serving = await WebHost.Serve();
        var service = ClientServices.GetRequiredService<IRestEaseClient>();

        var jsonString = (await service.ConcatQueryAndPath("ab", "cD"));
        jsonString.Value.Should().Be("abcD");
    }
    
    [Fact]
    public async Task ConcatPathAndBody()
    {
        await using var serving = await WebHost.Serve();
        var service = ClientServices.GetRequiredService<IRestEaseClient>();

        var jsonString = (await service.ConcatPathAndBody("ab", "cD"));
        jsonString.Value.Should().Be("abcD");
    }
}
