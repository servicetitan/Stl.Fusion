using System.Reactive.PlatformServices;
using Stl.Internal;

namespace Stl.Time.Testing;

public sealed class TestClock : ITestClock, IDisposable
{
    private volatile TestClockSettings _settings;

    public TestClockSettings Settings {
        get => _settings;
        set {
            if (!value.IsUsable)
                throw Errors.AlreadyUsed();
            var oldSettings = Interlocked.Exchange(ref _settings, value);
            oldSettings.Changed();
            oldSettings.Dispose();
        }
    }

    [JsonConstructor, Newtonsoft.Json.JsonConstructor]
    public TestClock(TestClockSettings settings)
        => _settings = settings;
    public TestClock(TimeSpan localOffset = default, TimeSpan realOffset = default, double multiplier = 1)
        => _settings = new TestClockSettings(localOffset, realOffset, multiplier);
    public void Dispose() => _settings.Dispose();

    public override string ToString()
        => $"{GetType().Name}({Settings.LocalOffset} + {Settings.Multiplier} * (t - {Settings.RealOffset}))";

    // Operations

    public Moment Now => ToLocalTime(SystemClock.Now);
    DateTimeOffset ISystemClock.UtcNow => Now;

    public Moment ToRealTime(Moment localTime) => Settings.ToRealTime(localTime);
    public Moment ToLocalTime(Moment realTime) => Settings.ToLocalTime(realTime);
    public TimeSpan ToRealDuration(TimeSpan localDuration) => Settings.ToLocalDuration(localDuration);
    public TimeSpan ToLocalDuration(TimeSpan realDuration) => Settings.ToRealDuration(realDuration);

    public async Task Delay(TimeSpan dueIn, CancellationToken cancellationToken = default)
    {
        if (dueIn == Timeout.InfiniteTimeSpan) {
            await Task.Delay(dueIn, cancellationToken).ConfigureAwait(false);
            return;
        }
        if (dueIn < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(dueIn));

        TestClockSettings? settings = Settings;
        var dueAt = settings.Now + dueIn;
        while (true) {
            settings ??= Settings;
            var settingsChangedToken = settings.ChangedToken;
            var delta = (settings.ToRealTime(dueAt) - CpuClock.Now).Positive();
            try {
                if (!cancellationToken.CanBeCanceled) {
                    await Task.Delay(delta, settingsChangedToken).ConfigureAwait(false);
                }
                else {
                    using var cts = cancellationToken.LinkWith(settingsChangedToken);
                    await Task.Delay(delta, cts.Token).ConfigureAwait(false);
                }
                return;
            }
            catch (OperationCanceledException) {
                if (!settingsChangedToken.IsCancellationRequested)
                    throw;
            }
            settings = null;
        }
    }
}
