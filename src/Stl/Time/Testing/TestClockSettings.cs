using System;
using System.Threading;

namespace Stl.Time.Testing
{
    public sealed class TestClockSettings : IDisposable
    {
        private CancellationTokenSource? _changedTokenSource;

        public TimeSpan LocalOffset { get; }
        public TimeSpan RealOffset { get; }
        public double Multiplier { get; }

        public Moment Now => ToLocalTime(CpuClock.Now);
        public CancellationToken ChangedToken { get; }
        public bool IsUsable => !(_changedTokenSource?.IsCancellationRequested ?? true);

        public TestClockSettings(
            TimeSpan localOffset = default,
            TimeSpan realOffset = default,
            double multiplier = 1)
        {
            LocalOffset = localOffset;
            RealOffset = realOffset;
            Multiplier = multiplier;
            _changedTokenSource = new CancellationTokenSource();
            ChangedToken = _changedTokenSource.Token;
        }

        public void Changed()
            => _changedTokenSource?.Cancel();

        public void Dispose()
        {
            _changedTokenSource?.Dispose();
            _changedTokenSource = null;
        }

        // Conversion

        public override string ToString() => $"{GetType().Name}({LocalOffset} + {Multiplier} * (t - {RealOffset}))";

        public void Deconstruct(out TimeSpan localOffset, out TimeSpan realOffset, out double multiplier)
        {
            localOffset = LocalOffset;
            realOffset = RealOffset;
            multiplier = Multiplier;
        }

        public static implicit operator TestClockSettings((TimeSpan LocalOffset, TimeSpan RealOffset, double Multiplier) settings)
            => new(settings.LocalOffset, settings.RealOffset, settings.Multiplier);

        // Other operations

        public Moment ToLocalTime(Moment realTime)
            => new(LocalOffset + (realTime.EpochOffset + RealOffset).Multiply(Multiplier));
        public Moment ToRealTime(Moment localTime)
            => new((localTime.EpochOffset - LocalOffset).Divide(Multiplier) - RealOffset);
        public TimeSpan ToLocalDuration(TimeSpan realDuration)
            => realDuration.Multiply(Multiplier);
        public TimeSpan ToRealDuration(TimeSpan localDuration)
            => localDuration.Divide(Multiplier);
    }
}
