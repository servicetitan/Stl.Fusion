using System;
using System.Reactive.PlatformServices;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Internal;

namespace Stl.Time.Testing
{
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

        DateTimeOffset ISystemClock.UtcNow => Now;
        public Moment Now => ToLocalTime(SystemClock.Now);

        public Moment ToRealTime(Moment localTime) => Settings.ToRealTime(localTime);
        public Moment ToLocalTime(Moment realTime) => Settings.ToLocalTime(realTime);
        public TimeSpan ToRealDuration(TimeSpan localDuration) => Settings.ToLocalDuration(localDuration);
        public TimeSpan ToLocalDuration(TimeSpan realDuration) => Settings.ToRealDuration(realDuration);

        public async Task Delay(TimeSpan dueIn, CancellationToken cancellationToken = default)
        {
            var isInfinite = dueIn == Timeout.InfiniteTimeSpan;
            if (dueIn < TimeSpan.Zero && !isInfinite)
                throw new ArgumentOutOfRangeException(nameof(dueIn));

            TestClockSettings? settings = Settings;
            var dueAt = settings.Now + dueIn;
            for (;;) {
                settings ??= Settings;
                var settingsChangedToken = settings.ChangedToken;
                var delta = settings.ToRealTime(dueAt) - CpuClock.Now;
                if (delta < TimeSpan.Zero)
                    delta = TimeSpan.Zero;
                if (isInfinite)
                    delta = Timeout.InfiniteTimeSpan;
                if (cancellationToken == default) {
                    await Task.Delay(delta, settingsChangedToken).SuppressCancellation().ConfigureAwait(false);
                    if (!settingsChangedToken.IsCancellationRequested)
                        break;
                }
                else {
                    using var lts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, settingsChangedToken);
                    await Task.Delay(delta, lts.Token).SuppressCancellation().ConfigureAwait(false);
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!settingsChangedToken.IsCancellationRequested)
                        break;
                }
                settings = null;
            }
        }
    }
}
