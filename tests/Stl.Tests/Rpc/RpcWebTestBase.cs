using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Stl.Fusion.Bridge;
using Stl.Fusion.Bridge.Messages;
using Stl.Fusion.EntityFramework;
using Stl.Fusion.Extensions;
using Stl.IO;
using Stl.Locking;
using Stl.RegisterAttributes;
using Stl.Rpc;
using Stl.Rpc.Server;
using Stl.Testing.Collections;
using Stl.Testing.Output;
using Xunit.DependencyInjection.Logging;

namespace Stl.Tests.Rpc;

public class RpcWebTestOptions
{
    public bool UseLogging { get; set; } = true;
}

[Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
public class RpbWebTestBase : TestBase, IAsyncLifetime
{
    private static readonly AsyncLock InitializeLock = new(ReentryMode.CheckedFail);
    protected static readonly ConcurrentDictionary<Symbol, string> ReplicaCache = new();

    public RpcWebTestOptions Options { get; }
    public bool IsLoggingEnabled { get; set; } = true;
    public RpcTestWebHost WebHost { get; }
    public IServiceProvider Services { get; }
    public IServiceProvider WebServices => WebHost.Services;
    public IServiceProvider ClientServices { get; }
    public ILogger Log { get; }

    public RpbWebTestBase(ITestOutputHelper @out, RpcWebTestOptions? options = null) : base(@out)
    {
        Options = options ?? new RpcWebTestOptions();
        // ReSharper disable once VirtualMemberCallInConstructor
        Services = CreateServices();
        WebHost = Services.GetRequiredService<RpcTestWebHost>();
        ClientServices = CreateServices(true);
        if (Options.UseLogging)
            Log = Services.LogFor(GetType());
        else
            Log = NullLogger.Instance;
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
        var services = (IServiceCollection) new ServiceCollection();
        ConfigureServices(services, isClient);
        return services.BuildServiceProvider();
    }

    protected virtual void ConfigureServices(IServiceCollection services, bool isClient = false)
    {
        services.AddSingleton(Out);

        // Logging
        if (Options.UseLogging)
            services.AddLogging(logging => {
                var debugCategories = new List<string> {
                    "Stl.Rpc",
                    "Stl.CommandR",
                    "Stl.Tests",
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
            var webHost = (RpcTestWebHost?)WebHost;
            if (webHost == null)
                webHost = new RpcTestWebHost(services);
            services.AddSingleton(_ => webHost);
        }
        else {
            var rpcClient = rpc.AddClient();
            rpcClient.Configure(_ => RpcClient.Options.Default with {
                AddressResolver = _ => WebHost.ServerUri.ToString(),
            });
        }
    }
}
