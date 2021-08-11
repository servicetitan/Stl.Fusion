using System.Collections.Generic;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Stl.RegisterAttributes;
using Xunit;

namespace Stl.Tests.DependencyInjection
{
    public class SettingsTest
    {
        public interface ITestSettings
        {
            string Value { get; set; }
        }

        [RegisterSettings("TestSettings", Scope = nameof(SettingsTest))]
        [RegisterAlias(typeof(ITestSettings), typeof(TestSettings), Scope = nameof(SettingsTest))]
        public class TestSettings : ITestSettings
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
                .UseRegisterAttributeScanner()
                .WithScope(nameof(SettingsTest))
                .WithTypeFilter(new Regex(".*"))
                .RegisterFrom(typeof(bool).Assembly)
                .Register<TestSettings>().Register(new[] {typeof(TestSettings)})
                    .Services
                .BuildServiceProvider();
            var testSettings = services.GetRequiredService<TestSettings>();
            testSettings.Value.Should().Be("1");
            services.GetRequiredService<ITestSettings>().Should().BeSameAs(testSettings);
        }
    }
}
