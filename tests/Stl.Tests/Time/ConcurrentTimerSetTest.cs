using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Stl.OS;
using Stl.Testing;
using Stl.Time;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Collections
{
    [Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
    public class ConcurrentTimerSetTest : TestBase
    {
        public class Timer
        {
            public Moment DueAt { get; set; }
            public Moment FiredAt { get; set; }
        }

        public ConcurrentTimerSetTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public async Task BasicTest()
        {
            var clock = SystemClock.Instance;
            await using var timerSet = new ConcurrentTimerSet<Timer>(new ConcurrentTimerSet<Timer>.Options() {
                Quanta = TimeSpan.FromMilliseconds(10),
                Clock = clock,
                FireHandler = t => t.FiredAt = CoarseCpuClock.Now,
            });

            // AddOrUpdateToLater
            var t = new Timer();
            timerSet.AddOrUpdate(t, clock.Now + TimeSpan.FromMilliseconds(100));
            timerSet.AddOrUpdateToEarlier(t, clock.Now + TimeSpan.FromMilliseconds(200))
                .Should().BeFalse();
            timerSet.AddOrUpdateToLater(t, clock.Now + TimeSpan.FromMilliseconds(200))
                .Should().BeTrue();
            t.FiredAt.Should().Be(default);
            await clock.DelayAsync(150);
            t.FiredAt.Should().Be(default);
            await clock.DelayAsync(100);
            t.FiredAt.Should().NotBe(default);

            // AddOrUpdateToEarlier
            t = new Timer();
            timerSet.AddOrUpdate(t, clock.Now + TimeSpan.FromMilliseconds(200));
            timerSet.AddOrUpdateToLater(t, clock.Now + TimeSpan.FromMilliseconds(100))
                .Should().BeFalse();
            timerSet.AddOrUpdateToEarlier(t, clock.Now + TimeSpan.FromMilliseconds(100))
                .Should().BeTrue();
            t.FiredAt.Should().Be(default);
            await clock.DelayAsync(150);
            t.FiredAt.Should().NotBe(default);

            // Remove
            t = new Timer();
            timerSet.Remove(t).Should().BeFalse();
            timerSet.AddOrUpdate(t, clock.Now + TimeSpan.FromMilliseconds(100));
            timerSet.Remove(t).Should().BeTrue();
            t.FiredAt.Should().Be(default);
            await clock.DelayAsync(150);
            t.FiredAt.Should().Be(default);
        }

        [Fact]
        public async Task RandomTimerTest()
        {
            var taskCount = (TestRunnerInfo.IsBuildAgent() ? 1 : 10) * HardwareInfo.ProcessorCount;
            var maxDelta = TestRunnerInfo.IsBuildAgent() ? 2000 : 200;
            var rnd = new Random();
            var tasks = Enumerable.Range(0, taskCount)
                .Select(i => Task.Run(() => OneRandomTest(rnd.Next(100), 3000, maxDelta)))
                .ToArray();
            await Task.WhenAll(tasks);
        }

        [Fact(Skip = "Performance")]
        public async Task TimerPerformanceTest()
        {
            await using var timerSet = new ConcurrentTimerSet<Timer>(new ConcurrentTimerSet<Timer>.Options() {
                ConcurrencyLevel = HardwareInfo.ProcessorCountPo2 << 5,
                Quanta = TimeSpan.FromMilliseconds(100),
                FireHandler = t => t.FiredAt = CoarseCpuClock.Now,
            });
            var tasks = Enumerable.Range(0, HardwareInfo.ProcessorCount)
                .Select(i => Task.Run(() => OneRandomTest(timerSet, 500_000, 1000, 5000)))
                .ToArray();
            await Task.WhenAll(tasks);
        }

        private static async Task OneRandomTest(int timerCount, int maxDuration, int maxDelta)
        {
            await using var timerSet = new ConcurrentTimerSet<Timer>(new ConcurrentTimerSet<Timer>.Options() {
                Quanta = TimeSpan.FromMilliseconds(100),
                FireHandler = t => t.FiredAt = CoarseCpuClock.Now,
            });
            await OneRandomTest(timerSet, timerCount, maxDuration, maxDelta);
        }

        private static async Task OneRandomTest(ConcurrentTimerSet<Timer> timerSet, int timerCount, int maxDuration, int maxDelta)
        {
            var rnd = new Random();
            var start = CoarseCpuClock.Now + TimeSpan.FromSeconds(0.5);
            var timers = Enumerable
                .Range(0, timerCount)
                .Select(i => new Timer() {
                    DueAt = start + TimeSpan.FromMilliseconds(rnd.Next(maxDuration))
                })
                .ToList();
            foreach (var timer in timers)
                timerSet.AddOrUpdate(timer, timer.DueAt);

            await timerSet.Clock.DelayAsync(TimeSpan.FromMilliseconds(maxDuration));
            while (timerSet.Count != 0)
                await timerSet.Clock.DelayAsync(100);

            foreach (var timer in timers) {
                var delta = timer.FiredAt - timer.DueAt;
                delta.TotalMilliseconds.Should().BeInRange(-maxDelta, maxDelta);
            }
        }
    }
}
