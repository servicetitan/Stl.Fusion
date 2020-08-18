using System;
using Stl.OS;
using Stl.Time;

namespace Stl.Fusion.Internal
{
    public interface INotifyKeepAliveEnded
    {
        void KeepAliveEnded();
    }

    public static class RefHolder
    {
        private readonly static ConcurrentTimerSet<object> SimpleTimers;
        private readonly static ConcurrentTimerSet<INotifyKeepAliveEnded> NotifyingTimers;
        private readonly static IMomentClock Clock;

        static RefHolder()
        {
            Clock = CoarseCpuClock.Instance;
            var quanta = TimeSpan.FromMilliseconds(250);
            var concurrencyLevel = HardwareInfo.ProcessorCountPo2 << 5;
            SimpleTimers = new ConcurrentTimerSet<object>(
                new ConcurrentTimerSet<object>.Options() {
                    Quanta = quanta,
                    ConcurrencyLevel = concurrencyLevel,
                    Clock = Clock,
                });
            NotifyingTimers = new ConcurrentTimerSet<INotifyKeepAliveEnded>(
                new ConcurrentTimerSet<INotifyKeepAliveEnded>.Options() {
                    Quanta = quanta,
                    ConcurrencyLevel = concurrencyLevel,
                    Clock = Clock,
                    FireHandler = t => t.KeepAliveEnded(),
                });
        }

        public static void Hold(object target, int durationMs)
            => Hold(target, Clock.Now + TimeSpan.FromMilliseconds(durationMs));
        public static void Hold(object target, TimeSpan duration)
            => Hold(target, Clock.Now + duration);
        public static void Hold(object target, Moment until)
        {
            if (target is INotifyKeepAliveEnded notify)
                NotifyingTimers.AddOrUpdateToLater(notify, until);
            else
                SimpleTimers.AddOrUpdateToLater(target, until);
        }

        public static void Release(object target)
        {
            if (target is INotifyKeepAliveEnded notify)
                NotifyingTimers.Remove(notify);
            else
                SimpleTimers.Remove(target);
        }
    }
}
