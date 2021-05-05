using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Stl.DependencyInjection;
using Stl.Text;
using Xunit;

namespace Stl.Tests.DependencyInjection
{
    public class HostedServiceTest
    {
        [Service(Scope = nameof(HostedServiceTest))]
        [AddHostedService(Scope = nameof(HostedServiceTest))]
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
                .UseAttributeScanner(nameof(HostedServiceTest))
                    .AddServicesFrom(Assembly.GetExecutingAssembly())
                    .Services
                .BuildServiceProvider();
            var hostedServices = services.GetServices<IHostedService>().ToArray();
            hostedServices.Length.Should().Be(1);
            hostedServices[0].GetType().Should().Be(typeof(TestHostedService));
        }
    }
}
