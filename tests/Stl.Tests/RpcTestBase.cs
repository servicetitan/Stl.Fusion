using Stl.Diagnostics;
using Stl.Locking;
using Stl.RestEase;
using Stl.Rpc;
using Stl.Rpc.Clients;
using Stl.Testing.Collections;
using Stl.Testing.Output;
using Stl.Time.Testing;
using Xunit.DependencyInjection.Logging;

namespace Stl.Tests;

[Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
public abstract class RpcTestBase(ITestOutputHelper @out) : TestBase(@out), IAsyncLifetime
{
    private static readonly ReentrantAsyncLock InitializeLock = new(LockReentryMode.CheckedFail);
    protected static readonly RpcPeerRef ClientPeerRef = RpcPeerRef.Default;

    private IServiceProvider? _services;
    private IServiceProvider? _clientServices;
    private RpcWebHost? _webHost;
    private ILogger? _log;

    public bool UseLogging { get; init; } = true;
    public bool UseTestClock { get; init; }
    public bool IsLogEnabled { get; init; } = true;

    public IServiceProvider Services => _services ??= CreateServices();
    public IServiceProvider ClientServices => _clientServices ??= CreateServices(true);
    public IServiceProvider WebServices => WebHost.Services;
    public RpcWebHost WebHost => _webHost ??= Services.GetRequiredService<RpcWebHost>();
    public ILogger? Log => (_log ??= Services.LogFor(GetType())).IfEnabled(LogLevel.Debug, IsLogEnabled);

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
        if (UseTestClock)
            services.AddSingleton(new MomentClockSet(new TestClock()));
        services.AddSingleton(Out);

        // Logging
        if (UseLogging)
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
                    => debugCategories.Any(x => category?.StartsWith(x) ?? false)
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
            services.AddSingleton(_ => new RpcWebHost(services, GetType().Assembly));
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
