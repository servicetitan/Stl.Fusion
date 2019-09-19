using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Stl.Async;
using Stl.Internal;

namespace Stl.Time.Clocks
{
    public sealed class TestClock : ITestClock, IDisposable
    {
        private volatile TestClockSettings _settings;
        
        [JsonIgnore] public TestClockSettings Settings {
            get => _settings;
            set {
                if (!value.IsUsable)
                    throw Errors.AlreadyUsed();
                var oldSettings = Interlocked.Exchange(ref _settings, value);
                oldSettings.Changed();
                oldSettings.Dispose();
            }
        }

        public CancellationToken SettingsChangedToken => Settings.ChangedToken;

        public TestClock(TestClockSettings settings) 
            => _settings = settings;
        public TestClock(TimeSpan localOffset = default, TimeSpan realOffset = default, double multiplier = 1) 
            => _settings = new TestClockSettings(localOffset, realOffset, multiplier);
        public void Dispose() => _settings.Dispose();

        public override string ToString() 
            => $"{GetType().Name}({Settings.LocalOffset} + {Settings.Multiplier} * (t - {Settings.RealOffset}))";
        
        // Operations

        public Moment Now => ToLocalTime(RealTimeClock.Now);

        public Moment ToRealTime(Moment localTime) => Settings.ToRealTime(localTime);
        public Moment ToLocalTime(Moment realTime) => Settings.ToLocalTime(realTime);
        public TimeSpan ToRealTime(TimeSpan localDuration) => Settings.ToLocalTime(localDuration);
        public TimeSpan ToLocalTime(TimeSpan realDuration) => Settings.ToRealTime(realDuration);

        public async Task Delay(Moment dueAt, CancellationToken cancellationToken = default)
        {
            while (true) {
                var s = Settings;
                var settingsChangedToken = SettingsChangedToken;
                var delta = s.ToRealTime(dueAt) - RealTimeClock.Now;
                if (delta < TimeSpan.Zero)
                    delta = TimeSpan.Zero;
                Debug.WriteLine(delta);
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
            }
        }
    }
}
