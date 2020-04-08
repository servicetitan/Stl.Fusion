using System;
using Stl;
using Stl.Collections.Slim;
using Stl.Reactionist.Internal;

namespace Stl.Reactionist
{
    public interface IReactive
    {
        Disposable<(ReactiveBase, Reaction)> React(Reaction reaction);
        bool AddReaction(Reaction reaction);
        bool RemoveReaction(Reaction reaction);
    }

    public abstract class ReactiveBase : IReactive
    {
        public abstract bool AddReaction(Reaction reaction);
        public abstract bool RemoveReaction(Reaction reaction);
        
        public Disposable<(ReactiveBase, Reaction)> React(Reaction reaction)
        {
            if (!AddReaction(reaction))
                return default;
            return Disposable.New(
                state => state.Self.RemoveReaction(state.Reaction),
                (Self: this, Reaction: reaction));
        }
        
        protected void RegisterDependency()
        {
            var currentTracker = DependencyTracking.CurrentTracker;
            currentTracker?.RegisterDependency(this);
        }
    }
    
    public abstract class ReactiveWithReactionsBase : ReactiveBase
    {
        // ReSharper disable once MemberCanBePrivate.Global
        protected static readonly Aggregator<(Event, Exception?), Reaction> TriggerReactionsAggregator = TriggerReactions;

        private SafeHashSetSlim2<Reaction> _reactions = new SafeHashSetSlim2<Reaction>();

        public override bool AddReaction(Reaction reaction) => _reactions.Add(reaction);
        public override bool RemoveReaction(Reaction reaction) => _reactions.Remove(reaction);

        // Protected methods
        
        protected void TriggerReactions(Event @event)
        {
            var state = (Event: @event, Error: (Exception?) null);
            // This is done to minimize / have zero allocations
            _reactions.Aggregate(ref state, TriggerReactionsAggregator);
            if (state.Error != null)
                throw state.Error;
        }

        // Static method that's called during the aggregation
        private static void TriggerReactions(ref (Event Event, Exception? Error) _state, Reaction reaction)
        {
            try {
                reaction.Invoke(_state.Event);
            }
            catch (Exception e) {
                // We rethrow the first error here;
                // probably should modify this to throw AggregateException
                if (_state.Error == null)
                    _state = (_state.Event, e);
            }
        }

        protected void ClearReactions() => _reactions.Clear();
    }
}
