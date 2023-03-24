using Stl.Interception;

namespace Stl.Tests.Interception;

#pragma warning disable MA0042
#pragma warning disable VSTHRD103

public class TypeViewFactoryTest
{
    [Fact]
    public async Task CombinedTest()
    {
        var services = new ServiceCollection()
            .AddSingleton<Service>()
            .AddSingleton<IService, Service>()
            .BuildServiceProvider();

        async Task Test(IView view)
        {
            // ReSharper disable once MethodHasAsyncOverload
            view.One("").Should().Be("");
            // ReSharper disable once MethodHasAsyncOverload
            view.One("a").Should().Be("a");
            // ReSharper disable once MethodHasAsyncOverload
            view.Two("").Should().Be("0");
            // ReSharper disable once MethodHasAsyncOverload
            view.Two("a").Should().Be("1");
            // ReSharper disable once MethodHasAsyncOverload
            view.Three().Should().Be("1");

            (await view.OneAsync("")).Should().Be("");
            (await view.OneAsync("a")).Should().Be("a");
            (await view.TwoAsync("")).Should().Be("0");
            (await view.TwoAsync("a")).Should().Be("1");
            (await view.ThreeAsync()).Should().Be("1");

            (await view.OneXAsync("")).Should().Be("");
            (await view.OneXAsync("a")).Should().Be("a");
            (await view.TwoXAsync("")).Should().Be("0");
            (await view.TwoXAsync("a")).Should().Be("1");
            (await view.ThreeXAsync()).Should().Be("1");
        }

        var viewFactory = services.TypeViewFactory<IView>();
        var classView = viewFactory.CreateView(services.GetRequiredService<Service>());
        var interfaceView = viewFactory.CreateView(services.GetRequiredService<IService>());
        await Test(classView);
        await Test(interfaceView);
    }
}
