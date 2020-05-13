using System;
using System.Diagnostics;
using System.Threading;
using Stl.Fusion.Messages;
using Stl.Time;

namespace Stl.Fusion.Events
{
    public class PublicationInvalidatedEvent : PublicationStateChangedEvent
    {
        private volatile int _nextUpdateTimeUnits;

        public IntMoment NextUpdateTime {
            get => new IntMoment(_nextUpdateTimeUnits);
            set => Interlocked.Exchange(ref _nextUpdateTimeUnits, value.EpochOffsetUnits);
        }

        public PublicationInvalidatedEvent(IPublication publication, PublicationMessage? message)
            : this(publication, message, IntMoment.MaxValue) { }
        public PublicationInvalidatedEvent(IPublication publication, PublicationMessage? message, IntMoment nextUpdateTime)
            : base(publication, message)
        {
            _nextUpdateTimeUnits = nextUpdateTime.EpochOffsetUnits;
            Debug.Assert(publication.State == PublicationState.Invalidated);
        }

        public bool VoteForNextUpdateTime(IntMoment nextUpdateTime)
        {
            // Not sure if this is a better option than lock here, but let's try.
            var spinWait = new SpinWait();
            var nextUpdateTimeUnits = nextUpdateTime.EpochOffsetUnits;
            var c = _nextUpdateTimeUnits;
            while (true) {
                if (c <= nextUpdateTimeUnits)
                    return false;
                var nc = Interlocked.CompareExchange(ref _nextUpdateTimeUnits, nextUpdateTimeUnits, c);
                if (c == nc)
                    return true;
                c = nc;
                spinWait.SpinOnce();
            }
        }

        public bool VoteForNextUpdateTime(TimeSpan nextUpdateDelay)
        {
            var nextUpdateTime = IntMoment.Now + (int) (nextUpdateDelay.Ticks / IntMoment.TicksPerUnit);
            return VoteForNextUpdateTime(nextUpdateTime);
        }
    }
}
