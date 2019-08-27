using System;
using System.CommandLine;
using System.IO;
using System.Threading;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Stl.Plugins;
using Stl.Plugins.Extensions.Hosting;
using Stl.Testing;
using Stl.Tests.Plugins.Extensions.AppHost;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Plugins.Extensions
{
    public class AppHostTest : TestBase
    {
        protected string Url = WebHostTestEx.GetRandomLocalUrl();
        protected TestConsole Console = new TestConsole();

        public AppHostTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public void AutoStartTest()
        {
            using var host = CreateHost("");
            var output = Console.Out?.ToString()?.Trim() ?? "";
            output.Should().Contain("AutoStarted");
            output.Should().NotContain("Serving");
        }

        [Fact]
        public void WebHostTest()
        {
            using var host = CreateHost("serve");
            var output = Console.Out?.ToString()?.Trim() ?? "";
            output.Should().Contain("AutoStarted");
            output.Should().ContainAll("Serving");
        }

        public TestAppHost CreateHost(string arguments, CancellationToken cancellationToken = default)
        {
            var writer = new StringWriter();
            var log = TestLogger.New(writer);
            var loggerFactory = new LoggerFactory().AddSerilog(log);

            return new AppHostBuilder<TestAppHost>()
                .ConfigurePlugins(pluginHostBuilder => {
                    pluginHostBuilder.ConfigureServices(services => {
                        services.AddSingleton(loggerFactory);
                        services.AddSingleton<IConsole>(Console);
                        return services;
                    });
                    return pluginHostBuilder;
                })
                .ConfigureWebHost(webHostBuilder => {
                    webHostBuilder
                        .ConfigureServices(services => {
                            services.AddSingleton(loggerFactory);
                            services.AddSingleton<IConsole>(Console);
                        })
                        .UseUrls(Url);
                    return webHostBuilder;
                })
                .BuildAndStart(arguments, cancellationToken);
        }
    }
}
