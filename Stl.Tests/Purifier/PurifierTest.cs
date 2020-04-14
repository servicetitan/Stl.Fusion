using System;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Extras.DynamicProxy;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stl.Concurrency;
using Stl.Purifier;
using Stl.Purifier.Autofac;
using Stl.Testing;
using Stl.Testing.Internal;
using Stl.Tests.Purifier.Model;
using Stl.Tests.Purifier.Services;
using Xunit;
using Xunit.Abstractions;
using Xunit.DependencyInjection.Logging;

namespace Stl.Tests.Purifier
{
    public class PurifierTest : TestBase, IAsyncLifetime
    {
        public TestDbContext DbContext { get; set; }

        public PurifierTest(ITestOutputHelper @out) : base(@out)
        {
            DbContext = CreateDbContext();
        }

        public Task InitializeAsync() 
            => DbContext.Database.EnsureCreatedAsync();
        public Task DisposeAsync() 
            => Task.CompletedTask;

        public TestDbContext CreateDbContext() 
            => new TestDbContext(new DbContextOptionsBuilder()
                .LogTo(Out.WriteLine, LogLevel.Information)
                .Options);

        public IServiceProvider CreateServices()
        {
            var builder = new ContainerBuilder();
            // Logging
            builder.RegisterType<LoggerFactory>()
                .As<ILoggerFactory>()
                .SingleInstance();
            builder.RegisterGeneric(typeof(Logger<>))
                .As(typeof(ILogger<>))
                .SingleInstance();
            builder.Register(c => new XunitTestOutputLoggerProvider(new SimpleTestOutputHelperAccessor(Out)))
                .As<ILoggerProvider>()
                .SingleInstance();
            // Interceptors
            builder.Register(c => new ConcurrentIdGenerator<long>(i => {
                var id = i * 10000;
                return () => ++id;
            }));
            builder.RegisterType<ComputedRegistry<(IFunction, InvocationInput)>>()
                .As<IComputedRegistry<(IFunction, InvocationInput)>>();
            builder.RegisterType<ComputedInterceptor>();
            // Services
            builder.RegisterType<TimeProvider>()
                .As<ITimeProvider>()
                .SingleInstance();
            builder.RegisterType<TimeProvider>()
                .As<ITimeProviderEx>()
                .EnableInterfaceInterceptors()
                .InterceptedBy(typeof(ComputedInterceptor))
                .SingleInstance();
            var container = builder.Build();
            return new AutofacServiceProvider(container);
        }

        [Fact]
        public async Task BasicTest()
        {
            var count = await DbContext.Users.CountAsync();
            count.Should().Be(0);

            var u1 = new User() {
                Id = 1,
                Name = "AY"
            };
            var p1 = new Post() {
                Id = 2,
                Title = "Test",
                Author = u1,
            };
            u1.Posts.Add(p1.Key, p1);
            await DbContext.Users.AddAsync(u1);
            await DbContext.Posts.AddAsync(p1);
            await DbContext.SaveChangesAsync();

            DbContext = CreateDbContext();
            (await DbContext.Users.CountAsync()).Should().Be(1);
            (await DbContext.Posts.CountAsync()).Should().Be(1);
            u1 = await DbContext.Users.FindAsync(u1.Id);
            u1.Name.Should().Be("AY");
            p1 = await DbContext.Posts
                .Where(p => p.Id == p.Id)
                .Include(p => p.Author)
                .SingleAsync();
            p1.Author.Id.Should().Be(u1.Id);
            // u.Posts.Count().Should().Be(1);
        }

        [Fact]
        public async Task BasicContainerTest()
        {
            var c = CreateServices().GetRequiredService<ILifetimeScope>();
            var tp = c.Resolve<ITimeProvider>();                                                      
            var tpe = c.Resolve<ITimeProviderEx>();
            var cNow = await tpe.GetTimeAsync();
            using (var o = cNow.AutoRecompute()) {
                using var _ = o.Subscribe(c => Out.WriteLine($"-> {c.Value}"));
                await Task.Delay(2000);
            }
            Out.WriteLine("Disposed.");
            await Task.Delay(2000);
            Out.WriteLine("Finished.");
        }
    }
}
