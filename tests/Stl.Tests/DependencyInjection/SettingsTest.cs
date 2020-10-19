using System.Collections.Generic;
using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Stl.DependencyInjection;
using Xunit;

namespace Stl.Tests.DependencyInjection
{
    public class SettingsTest
    {
        [Settings("TestSettings", Scope = nameof(SettingsTest))]
        public class TestSettings
        {
            public string Value { get; set; } = "";
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
                .AttributeBased(nameof(SettingsTest))
                .AddServicesFrom(Assembly.GetExecutingAssembly())
                .BackToServices()
                .BuildServiceProvider();
            var testSettings = services.GetRequiredService<TestSettings>();
            testSettings.Value.Should().Be("1");
        }
    }
}
