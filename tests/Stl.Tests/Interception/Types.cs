using Stl.Interception;

namespace Stl.Tests.Interception;

public interface IService
{
    string One(string source);
    int Two(string source);
    JsonString Three();

    Task<string> OneAsync(string source);
    Task<int> TwoAsync(string source);
    Task<JsonString> ThreeAsync();

    ValueTask<string> OneXAsync(string source);
    ValueTask<int> TwoXAsync(string source);
    ValueTask<JsonString> ThreeXAsync();
}

public interface IView : IRequiresFullProxy
{
    string One(string source);
    string Two(string source);
    string Three();

    Task<string> OneAsync(string source);
    Task<string> TwoAsync(string source);
    Task<string> ThreeAsync();

    ValueTask<string> OneXAsync(string source);
    ValueTask<string> TwoXAsync(string source);
    ValueTask<string> ThreeXAsync();
}

public class Service : IService
{
    public string One(string source)
        => source;

    public int Two(string source)
        => source.Length;

    public JsonString Three()
        => new("1");

    public Task<string> OneAsync(string source)
        => Task.FromResult(One(source));

    public Task<int> TwoAsync(string source)
        => Task.FromResult(Two(source));

    public Task<JsonString> ThreeAsync()
        => Task.FromResult(Three());

    public ValueTask<string> OneXAsync(string source)
        => ValueTaskExt.FromResult(One(source));

    public ValueTask<int> TwoXAsync(string source)
        => ValueTaskExt.FromResult(Two(source));

    public ValueTask<JsonString> ThreeXAsync()
        => ValueTaskExt.FromResult(Three());
}
