using Stl.Rpc;
using Stl.Rpc.Testing;
using Stl.Testing.Output;
using Xunit.DependencyInjection.Logging;

namespace Stl.Tests.Rpc;

public abstract class RpcLocalTestBase : TestBase
{
    protected RpcTestConnection Connection;

    protected RpcLocalTestBase(ITestOutputHelper @out) : base(@out) { }

    protected virtual IServiceProvider CreateServices(
        Action<IServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        configureServices?.Invoke(services);

        var c = services.BuildServiceProvider();
        StartServices(c);
        return c;
    }

    protected virtual void StartServices(IServiceProvider services)
    {
        var testClient = services.GetRequiredService<RpcTestClient>();
        Connection = testClient.CreateDefault();
        Connection.Connect();
    }

    protected virtual void ConfigureServices(ServiceCollection services)
    {
        services.AddLogging(logging => {
            logging.ClearProviders();
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddDebug();
            logging.AddProvider(
#pragma warning disable CS0618
                new XunitTestOutputLoggerProvider(
                    new TestOutputHelperAccessor(Out),
                    (_, level) => level >= LogLevel.Debug));
#pragma warning restore CS0618
        });

        var rpc = services.AddRpc();
        rpc.AddTestClient();
    }
}
