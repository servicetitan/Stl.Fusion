using Stl.OS;
using Stl.Testing.Collections;

namespace Stl.Tests.Time;

[Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
public class ConcurrentTimerSetTest : TestBase
{
    public class Timer
    {
        private readonly ThreadSafe<Moment> _firedAt = new();

        public Moment DueAt { get; init; }

        public Moment FiredAt {
            get => _firedAt.Value;
            set => _firedAt.Value = value;
        }
    }

    private int _runnerId;

    public ConcurrentTimerSetTest(ITestOutputHelper @out) : base(@out) { }

    [Fact]
    public async Task BasicTest()
    {
        var clock = MomentClockSet.Default.CpuClock;
        await using var timerSet = new ConcurrentTimerSet<Timer>(
            new() {
                Quanta = TimeSpan.FromMilliseconds(10),
                Clock = clock,
            },
            timer => timer.FiredAt = clock.Now);

        // AddOrUpdateToLater
        var t = new Timer();
        timerSet.AddOrUpdate(t, clock.Now + TimeSpan.FromMilliseconds(100));
        timerSet.AddOrUpdateToEarlier(t, clock.Now + TimeSpan.FromMilliseconds(200))
            .Should().BeFalse();
        timerSet.AddOrUpdateToLater(t, clock.Now + TimeSpan.FromMilliseconds(200))
            .Should().BeTrue();
        t.FiredAt.Should().Be(default);
        await clock.Delay(50);
        t.FiredAt.Should().Be(default);
        await clock.Delay(250);
        t.FiredAt.Should().NotBe(default);

        // AddOrUpdateToEarlier
        t = new Timer();
        timerSet.AddOrUpdate(t, clock.Now + TimeSpan.FromMilliseconds(500));
        timerSet.AddOrUpdateToLater(t, clock.Now + TimeSpan.FromMilliseconds(200))
            .Should().BeFalse();
        timerSet.AddOrUpdateToEarlier(t, clock.Now + TimeSpan.FromMilliseconds(200))
            .Should().BeTrue();
        t.FiredAt.Should().Be(default);
        await clock.Delay(300);
        t.FiredAt.Should().NotBe(default);

        // Remove
        t = new Timer();
        timerSet.Remove(t).Should().BeFalse();
        timerSet.AddOrUpdate(t, clock.Now + TimeSpan.FromMilliseconds(100));
        timerSet.Remove(t).Should().BeTrue();
        t.FiredAt.Should().Be(default);
        await clock.Delay(200);
        t.FiredAt.Should().Be(default);
    }

    [Fact]
    public async Task RemoveTest()
    {
        var clock = MomentClockSet.Default.CpuClock;
        await using var timerSet = new ConcurrentTimerSet<Timer>(
            new() {
                Quanta = TimeSpan.FromMilliseconds(10),
                Clock = clock,
            },
            timer => timer.FiredAt = clock.Now);

        var t = new Timer();
        timerSet.Remove(t).Should().BeFalse();
        timerSet.AddOrUpdate(t, clock.Now + TimeSpan.FromMilliseconds(100));
        timerSet.Remove(t).Should().BeTrue();
        t.FiredAt.Should().Be(default);
        await clock.Delay(200);
        t.FiredAt.Should().Be(default);
    }

    [Fact]
    public async Task RandomTimerTest()
    {
        var taskCount = TestRunnerInfo.IsBuildAgent() ? 1 : HardwareInfo.GetProcessorCountFactor(10);
        var maxDelta = 1000;
        var rnd = new Random();
        var tasks = Enumerable.Range(0, (int) MathExt.Max(taskCount / 10, 1))
            .Select(_ => Task.Run(() => OneRandomTest(rnd.Next(100), 3000, maxDelta)))
            .ToArray();
        await Task.WhenAll(tasks).SilentAwait();
        Enumerable.Range(0, taskCount)
            .Select(_ => Task.Run(() => OneRandomTest(rnd.Next(100), 3000, maxDelta)))
            .ToArray();
        await Task.WhenAll(tasks);
    }

    // [Fact]
    [Fact(Skip = "Performance")]
    public async Task TimerPerformanceTest()
    {
        var clock = MomentClockSet.Default.CoarseCpuClock;
        var timerSet = new ConcurrentTimerSet<Timer>(
            new() {
                Quanta = TimeSpan.FromMilliseconds(100),
            },
            timer => timer.FiredAt = clock.Now);
        await using (timerSet) {
            var tasks = Enumerable.Range(0, HardwareInfo.GetProcessorCountFactor())
                .Select(_ => Task.Run(() => OneRandomTest(timerSet, 100_000, 5000, 1000)))
                .ToArray();
            await Task.WhenAll(tasks);
            Out.WriteLine("ConcurrentTimerSet is disposing...");
        }
        Out.WriteLine("ConcurrentTimerSet is disposed.");
    }

    private async Task OneRandomTest(int timerCount, int maxDuration, int maxDelta)
    {
        var clock = MomentClockSet.Default.CoarseCpuClock;
        await using var timerSet = new ConcurrentTimerSet<Timer>(
            new() {
                Quanta = TimeSpan.FromMilliseconds(100),
            },
            timer => timer.FiredAt = clock.Now);
        await OneRandomTest(timerSet, timerCount, maxDuration, maxDelta);
    }

    private async Task OneRandomTest(ConcurrentTimerSet<Timer> timerSet, int timerCount, int maxDuration, int maxDelta)
    {
        var runner = $"Runner {Interlocked.Increment(ref _runnerId)}";
        var rnd = new Random();
        var clock = timerSet.Clock;
        var start = clock.Now + TimeSpan.FromSeconds(0.5);
        var timers = Enumerable
            .Range(0, timerCount)
            .Select(_ => new Timer() {
                DueAt = start + TimeSpan.FromMilliseconds(rnd.Next(maxDuration))
            })
            .ToList();
        Out.WriteLine($"{runner}: Timers created.");

        foreach (var timer in timers)
            timerSet.AddOrUpdate(timer, timer.DueAt);
        Out.WriteLine($"{runner}: Timers enqueued.");

        var maxDueAt = timers.Any() ? timers.Max(t => t.DueAt) : start;
        await clock.Delay(maxDueAt);
        await TestExt.WhenMet(() => timerSet.Count.Should().Be(0), TimeSpan.FromSeconds(1));
        Out.WriteLine($"{runner}: All timers should be fired.");

        foreach (var timer in timers) {
            var delta = timer.FiredAt - timer.DueAt;
            delta.Should().BeCloseTo(TimeSpan.Zero, TimeSpan.FromMilliseconds(maxDelta));
        }
        Out.WriteLine($"{runner}: Checks completed.");
    }
}
