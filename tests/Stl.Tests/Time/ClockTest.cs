using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Stl.Async;
using Stl.Collections;
using Stl.Testing;
using Stl.Time;
using Stl.Time.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Time
{
    [Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
    public class ClockTest : TestBase
    {
        public ClockTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public async Task BasicTest()
        {
            var epsilon = TimeSpan.FromSeconds(1);
            var epsilon10 = epsilon * 10;
            using var clock = new TestClock().SpeedupBy(10).OffsetBy(1000);
            var realStart = SystemClock.Now;
            var clockStart = clock.Now;

            ShouldEqual(realStart, DateTime.Now.ToMoment(), epsilon);
            ShouldEqual(realStart, clock.ToRealTime(clockStart), epsilon);
            ShouldEqual(clockStart, clock.ToLocalTime(realStart), epsilon10);
            ShouldEqual(clockStart, realStart + TimeSpan.FromSeconds(1), epsilon);

            await clock.Delay(TimeSpan.FromSeconds(5));
            ShouldEqual(realStart + TimeSpan.FromSeconds(0.5), SystemClock.Now, epsilon);
            ShouldEqual(clockStart + TimeSpan.FromSeconds(5), clock.Now, epsilon10);
            Out.WriteLine(clock.Now.ToString());

            clock.OffsetBy(1000);
            ShouldEqual(clockStart + TimeSpan.FromSeconds(6), clock.Now, epsilon10);

            clock.SpeedupBy(0.1);
            await clock.Delay(TimeSpan.FromSeconds(0.5));
            ShouldEqual(realStart + TimeSpan.FromSeconds(1), SystemClock.Now, epsilon);
            ShouldEqual(clockStart + TimeSpan.FromSeconds(6.5), clock.Now, epsilon10);
        }

        [Fact]
        public async Task TimerTest1()
        {
            var epsilon = TimeSpan.FromSeconds(0.9);
            var epsilon10 = epsilon * 10;
            using var clock = new TestClock().SpeedupBy(10).OffsetBy(1000);
            var realStart = SystemClock.Now;
            var clockStart = clock.Now;

            var firedAt = clock.Timer(1000).Select(i => clock.Now).ToEnumerable().Single();
            ShouldEqual(firedAt, clockStart + TimeSpan.FromSeconds(1), epsilon10);

            await Task.Yield(); // Just to suppress warning.
        }

        [Fact]
        public async Task TimerTest2()
        {
            var epsilon = TimeSpan.FromSeconds(0.9);
            var epsilon10 = epsilon * 10;
            using var clock = new TestClock().SpeedupBy(10).OffsetBy(1000);
            var realStart = SystemClock.Now;
            var clockStart = clock.Now;

            var m = 100.0;
            clock.SpeedupBy(1/m);
            var speedupTask = Task.Run<Task>(async () => {
                await Task.Delay(100);
                clock.SpeedupBy(m);
            });

            var firedAt = clock.Timer(1000).Select(i => SystemClock.Now).ToEnumerable().Single();
            // O.2 = 0.1s in Task.Delay + 0.1s to wait for the remainder of the timer,
            // b/c the end time was set when the clock was ticking 10x slower than normal
            ShouldEqual(firedAt, realStart + TimeSpan.FromSeconds(0.2), epsilon);

            await speedupTask;
        }

        [Fact]
        public async Task IntervalTest()
        {
            var epsilon = TimeSpan.FromSeconds(1);
            using var clock = new TestClock();
            var realStart = SystemClock.Now;
            var clockStart = clock.Now;

            var m = 10.0;
            clock.SpeedupBy(1/m);
            var speedupTask = Task.Run<Task>(async () => {
                await Task.Delay(350);
                clock.SpeedupBy(m);
            });

            var timings = clock.Interval(10)
                .Select(i => SystemClock.Now - realStart)
                .Take(10)
                .ToEnumerable().ToArray();
            var deltas = timings.Zip(timings.Skip(1), (a, b) => b - a).ToArray();

            Out.WriteLine(deltas.Select(d => (long) d.TotalMilliseconds).ToDelimitedString());

            foreach (var d in deltas.Take(2))
                ShouldEqual(TimeSpan.FromMilliseconds(100), d, epsilon);
            foreach (var d in deltas.Skip(3))
                ShouldEqual(TimeSpan.FromMilliseconds(10), d, epsilon);

            await speedupTask;
        }

        [Fact]
        public async Task SpecialValuesTest()
        {
            async Task Test(IMomentClock clock1)
            {
                // Negative value (but not infinity)
                await ((Func<Task>) (async () => {
                    var cts = new CancellationTokenSource(100);
                    await clock1.Delay(-2, cts.Token);
                })).Should().ThrowAsync<ArgumentOutOfRangeException>();
                await ((Func<Task>) (async () => {
                    var cts = new CancellationTokenSource(100);
                    await clock1.Delay(TimeSpan.FromMilliseconds(-2), cts.Token);
                })).Should().ThrowAsync<ArgumentOutOfRangeException>();

                // Infinity
                await ((Func<Task>) (async () => {
                    var cts = new CancellationTokenSource(100);
                    await clock1.Delay(Timeout.Infinite, cts.Token).SuppressCancellation();
                })).Should().CompleteWithinAsync(TimeSpan.FromMilliseconds(1000));
                await ((Func<Task>) (async () => {
                    var cts = new CancellationTokenSource(100);
                    await clock1.Delay(Timeout.InfiniteTimeSpan, cts.Token).SuppressCancellation();
                })).Should().CompleteWithinAsync(TimeSpan.FromMilliseconds(1000));

                // Zero
                await ((Func<Task>) (async () => {
                    var cts = new CancellationTokenSource(1000);
                    await clock1.Delay(0, cts.Token).SuppressCancellation();
                })).Should().CompleteWithinAsync(TimeSpan.FromMilliseconds(500));
                await ((Func<Task>) (async () => {
                    var cts = new CancellationTokenSource(1000);
                    await clock1.Delay(TimeSpan.Zero, cts.Token).SuppressCancellation();
                })).Should().CompleteWithinAsync(TimeSpan.FromMilliseconds(500));
            }

            await Test(SystemClock.Instance);
            await Test(new TestClock());
        }

        protected static void ShouldEqual(TimeSpan a, TimeSpan b, TimeSpan epsilon)
            => Math.Abs((a - b).Ticks).Should().BeLessThan(epsilon.Ticks);

        protected static void ShouldEqual(Moment a, Moment b, TimeSpan epsilon)
            => Math.Abs((a - b).Ticks).Should().BeLessThan(epsilon.Ticks);
    }
}
