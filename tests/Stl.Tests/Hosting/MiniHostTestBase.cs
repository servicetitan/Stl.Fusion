using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Stl.Testing;
using Stl.Time.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Hosting
{
    public abstract class MiniHostTestBase : TestBase, IAsyncLifetime
    {
        protected TestMiniHostProvider HostProvider { get; set; }
        protected IHost Host => HostProvider.Host;
        protected Uri HostUrl => HostProvider.HostUrl;
        protected IServiceProvider Services => Host.Services;
        protected ITestClock Clock => HostProvider.Clock;
        protected TestOutputConsole Console => HostProvider.Console;

        protected MiniHostTestBase(ITestOutputHelper @out) : base(@out) 
            => HostProvider = new TestMiniHostProvider(@out);

        Task IAsyncLifetime.InitializeAsync() => InitializeAsync();
        protected virtual Task InitializeAsync() 
            => CreateHostAsync();

        Task IAsyncLifetime.DisposeAsync() => DisposeAsync();
        protected virtual async Task DisposeAsync() 
            => await HostProvider.DisposeAsync();

        protected Task CreateHostAsync(params string[] arguments)
            => HostProvider.CreateHostAsync();
    }
}
