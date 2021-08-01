using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Stl.Async;
using Stl.Interception;
using Stl.Serialization;
using Xunit;

namespace Stl.Tests.Interception
{
    public class TypeViewFactoryTest
    {
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

        public interface IView
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
                => "1";

            public Task<string> OneAsync(string source)
                => Task.FromResult(One(source));

            public Task<int> TwoAsync(string source)
                => Task.FromResult(Two(source));

            public Task<JsonString> ThreeAsync()
                => Task.FromResult(Three());

            public ValueTask<string> OneXAsync(string source)
                => ValueTaskEx.FromResult(One(source));

            public ValueTask<int> TwoXAsync(string source)
                => ValueTaskEx.FromResult(Two(source));

            public ValueTask<JsonString> ThreeXAsync()
                => ValueTaskEx.FromResult(Three());
        }

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
}
