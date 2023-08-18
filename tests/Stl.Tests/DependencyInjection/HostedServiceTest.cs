using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Hosting;

namespace Stl.Tests.DependencyInjection;

public class HostedServiceTest
{
    public class TestHostedService : IHostedService
    {
        public bool IsStarted { get; set; }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            IsStarted = true;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            IsStarted = false;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public void BasicTest()
    {
        var services = CreateServices();
        var hostedServices = services.GetServices<IHostedService>().ToArray();
        hostedServices.Length.Should().Be(1);
        hostedServices[0].GetType().Should().Be(typeof(TestHostedService));
    }

    private static ServiceProvider CreateServices()
    {
        static KeyValuePair<string, string> NewPair(string key, string value)
            => new(key, value);

        var cfg = new ConfigurationBuilder()
            .Add(new MemoryConfigurationSource {
                InitialData = new[] {
                    NewPair("TestSettings:Value", "1"),
                }!
            })
            .Build();
        var section = cfg.GetSection("TestSettings");
        section["Value"].Should().Be("1");

        var services = new ServiceCollection()
            .AddSingleton(cfg)
            .AddSingleton<IConfiguration>(cfg);

        services.AddSingleton<TestHostedService>();
        services.AddHostedService<TestHostedService>();

        return services.BuildServiceProvider();
    }
}
