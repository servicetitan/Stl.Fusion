using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Stl.Async;
using Stl.Fusion.Authentication;
using Stl.Fusion.Tests.Services;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Fusion.Tests
{
    public class SessionParameterTest : SimpleFusionTestBase
    {
        public SessionParameterTest(ITestOutputHelper @out) : base(@out) { }

        protected override void ConfigureCommonServices(ServiceCollection services)
            => services.AddFusion().AddAuthentication();

        private static object syncObject = new Object();

        [Fact]
        public async Task BasicTest()
        {
            using var stopCts = new CancellationTokenSource();
            var cancellationToken = stopCts.Token;

            async Task Watch<T>(string name, IComputed<T> computed)
            {
                //for (;;) {
                //    Out.WriteLine($"{name}: {computed.Value}, {computed}");
                //    await computed.WhenInvalidated(cancellationToken);
                //    Out.WriteLine($"{name}: {computed.Value}, {computed}");
                //    computed = await computed.Update(false, cancellationToken);
                //}
            }

            var services = CreateServiceProviderFor<PerUserCounterService>();
            var counters = services.GetRequiredService<PerUserCounterService>();
            var sessionFactory = services.GetRequiredService<ISessionFactory>();
            var sessionA = sessionFactory.CreateSession();
            var sessionB = sessionFactory.CreateSession();
            
            Stl.Fusion.Internal.ComputedLog.LogAction = m => {
                lock (syncObject) {
                    Out.WriteLine(Thread.CurrentThread.ManagedThreadId.ToString("000") + "      " + m);
                }
            };

            var session = sessionA;
            var aaComputed = await Computed.Capture(_ => counters.Get("a", session));
            Task.Run(() => Watch(nameof(aaComputed), aaComputed)).Ignore();
            var abComputed = await Computed.Capture(_ => counters.Get("b", session));
            Task.Run(() => Watch(nameof(abComputed), abComputed)).Ignore();

            session = sessionB;
            var baComputed = await Computed.Capture(_ => counters.Get("a", session));
            Task.Run(() => Watch(nameof(baComputed), baComputed)).Ignore();

            session = sessionA;
            
            

            void Log(string message)
            {
                lock (syncObject) {
                    Out.WriteLine($"{Thread.CurrentThread.ManagedThreadId:000} (main): " + message);
                }
            }

            void LogComp(string computedName, IComputed computed)
            {
                Log($"{computedName}: {computed.Value}, {computed}");
            }
            
            LogComp(nameof(aaComputed), aaComputed);
            
            aaComputed.Invalidated += c => {
                Log($"{nameof(aaComputed)} invalidating.");
            };
            
            Log("about to increment 'a'");
            
            await counters.Increment("a", session);
            
            LogComp(nameof(aaComputed), aaComputed);
            
            (await aaComputed.Update(false)).Value.Should().Be(1, "aaComputed should be incremented");
            (await abComputed.Update(false)).Value.Should().Be(0);
            (await baComputed.Update(false)).Value.Should().Be(0);

            
            Log("step2");
            LogComp(nameof(abComputed), abComputed);
            
            abComputed.Invalidated += c => {
                Log($"{nameof(abComputed)} invalidating.");
            };
            
            Log("about to increment 'b'");
            await counters.Increment("b", session);
            
            Log("step3");
            
            LogComp(nameof(abComputed), abComputed);
            
            (await aaComputed.Update(false)).Value.Should().Be(1);
            (await abComputed.Update(false)).Value.Should().Be(1, "abComputed should be incremented");
            (await baComputed.Update(false)).Value.Should().Be(0);

            session = sessionB;
            await counters.Increment("a", session);
            (await aaComputed.Update(false)).Value.Should().Be(1);
            (await abComputed.Update(false)).Value.Should().Be(1);
            (await baComputed.Update(false)).Value.Should().Be(1);
            await counters.Increment("b", session);
            (await aaComputed.Update(false)).Value.Should().Be(1);
            (await abComputed.Update(false)).Value.Should().Be(1);
            (await baComputed.Update(false)).Value.Should().Be(1);

            stopCts.Cancel();
        }
    }
}
