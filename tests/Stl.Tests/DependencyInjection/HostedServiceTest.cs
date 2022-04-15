using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Hosting;
using Stl.RegisterAttributes;

namespace Stl.Tests.DependencyInjection;

public class HostedServiceTest
{
    [RegisterService(Scope = nameof(HostedServiceTest))]
    [RegisterHostedService(Scope = nameof(HostedServiceTest))]
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
        var cfg = new ConfigurationBuilder()
            .Add(new MemoryConfigurationSource {
                InitialData = new[] {
                    KeyValuePair.Create("TestSettings:Value", "1"),
                }
            })
            .Build();
        var section = cfg.GetSection("TestSettings");
        section["Value"].Should().Be("1");

        var services = new ServiceCollection()
            .AddSingleton(cfg)
            .AddSingleton<IConfiguration>(cfg)
            .UseRegisterAttributeScanner(nameof(HostedServiceTest))
                .RegisterFrom(Assembly.GetExecutingAssembly())
                .Services
            .BuildServiceProvider();
        var hostedServices = services.GetServices<IHostedService>().ToArray();
        hostedServices.Length.Should().Be(1);
        hostedServices[0].GetType().Should().Be(typeof(TestHostedService));
    }
}
