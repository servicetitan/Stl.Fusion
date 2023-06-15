using Stl.Locking;
using Stl.RestEase;
using Stl.Rpc;
using Stl.Rpc.Clients;
using Stl.Testing.Collections;
using Stl.Testing.Output;
using Stl.Time.Testing;
using Xunit.DependencyInjection.Logging;

namespace Stl.Tests;

public class RpcTestOptions
{
    public bool UseLogging { get; set; } = true;
    public bool UseTestClock { get; set; }
}

[Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
public abstract class RpcTestBase : TestBase, IAsyncLifetime
{
    private static readonly ReentrantAsyncLock InitializeLock = new(LockReentryMode.CheckedFail);
    protected static readonly Symbol ClientPeerId = RpcDefaults.DefaultPeerId;

    public RpcTestOptions Options { get; }
    public bool IsLoggingEnabled { get; set; } = true;
    public RpcWebHost WebHost { get; }
    public IServiceProvider Services { get; }
    public IServiceProvider WebServices => WebHost.Services;
    public IServiceProvider ClientServices { get; }
    public ILogger Log { get; }

    protected RpcTestBase(ITestOutputHelper @out, RpcTestOptions? options = null) : base(@out)
    {
        Options = options ?? new RpcTestOptions();
        // ReSharper disable once VirtualMemberCallInConstructor
        Services = CreateServices();
        WebHost = Services.GetRequiredService<RpcWebHost>();
        ClientServices = CreateServices(true);
        Log = Options.UseLogging
            ? Services.LogFor(GetType())
            : NullLogger.Instance;
    }

    public override async Task InitializeAsync()
    {
        using var __ = await InitializeLock.Lock().ConfigureAwait(false);
        await Services.HostedServices().Start();
    }

    public override async Task DisposeAsync()
    {
        if (ClientServices is IAsyncDisposable adcs)
            await adcs.DisposeAsync();
        if (ClientServices is IDisposable dcs)
            dcs.Dispose();

        try {
            await Services.HostedServices().Stop();
        }
        catch {
            // Intended
        }

        if (Services is IAsyncDisposable ads)
            await ads.DisposeAsync();
        if (Services is IDisposable ds)
            ds.Dispose();
    }

    protected IServiceProvider CreateServices(bool isClient = false)
    {
        var services = (IServiceCollection)new ServiceCollection();
        ConfigureServices(services, isClient);
        ConfigureTestServices(services, isClient);
        return services.BuildServiceProvider();
    }

    protected virtual void ConfigureTestServices(IServiceCollection services, bool isClient)
    { }

    protected virtual void ConfigureServices(IServiceCollection services, bool isClient)
    {
        if (Options.UseTestClock)
            services.AddSingleton(new MomentClockSet(new TestClock()));
        services.AddSingleton(Out);

        // Logging
        if (Options.UseLogging)
            services.AddLogging(logging => {
                var debugCategories = new List<string> {
                    "Stl.Rpc",
                    "Stl.Fusion",
                    "Stl.CommandR",
                    "Stl.Tests",
                    "Stl.Tests.Fusion",
                    // DbLoggerCategory.Database.Transaction.Name,
                    // DbLoggerCategory.Database.Connection.Name,
                    // DbLoggerCategory.Database.Command.Name,
                    // DbLoggerCategory.Query.Name,
                    // DbLoggerCategory.Update.Name,
                };

                bool LogFilter(string? category, LogLevel level)
                    => IsLoggingEnabled &&
                        debugCategories.Any(x => category?.StartsWith(x) ?? false)
                        && level >= LogLevel.Debug;

                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Debug);
                logging.AddFilter(LogFilter);
                logging.AddDebug();
                // XUnit logging requires weird setup b/c otherwise it filters out
                // everything below LogLevel.Information
                logging.AddProvider(
#pragma warning disable CS0618
                    new XunitTestOutputLoggerProvider(
                        new TestOutputHelperAccessor(Out),
                        LogFilter));
#pragma warning restore CS0618
            });

        var rpc = services.AddRpc();
        if (!isClient) {
            var webHost = (RpcWebHost?)WebHost ?? new RpcWebHost(services, GetType().Assembly);
            services.AddSingleton(_ => webHost);
            // rpc.UseWebSocketServer(); // Not necessary - RpcTestWebHost already does this
        }
        else {
            rpc.AddWebSocketClient(_ => RpcWebSocketClient.Options.Default with {
                HostUrlResolver = (_, _) => WebHost.ServerUri.ToString(),
            });
            var restEase = services.AddRestEase();
            restEase.ConfigureHttpClient((_, _, options) => {
                var apiUri = new Uri($"{WebHost.ServerUri}api/");
                options.HttpClientActions.Add(c => c.BaseAddress = apiUri);
            });
        }
    }
}
