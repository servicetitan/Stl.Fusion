using System;
using System.Collections.Generic;
using System.Threading;
using Stl;
using Stl.Reactionist.Internal;

namespace Stl.Reactionist
{
    public static class DependencyTracking
    {
        private static readonly AsyncLocal<DependencyTrackerBase> CurrentTrackerAsyncLocal = new AsyncLocal<DependencyTrackerBase>();
        
        public static DependencyTrackerBase CurrentTracker {
            get => CurrentTrackerAsyncLocal.Value;
            internal set => CurrentTrackerAsyncLocal.Value = value;
        }
    }
    
    public interface IDependencyTracker
    {
        bool IsActive { get; }
        IEnumerable<ReactiveBase> Dependencies { get; }

        Disposable<(DependencyTrackerBase, DependencyTrackerBase)> Activate();
        
        void RegisterDependency(ReactiveBase dependency);
        void ClearDependencies();
        // Generic virtual, so slow => must be avoided
        void ApplyToAllDependencies<TState>(TState state, Action<TState, ReactiveBase> action);
        void AddReactionToAllDependencies(Reaction reaction);
        void RemoveReactionFromAllDependencies(Reaction reaction);
    }
    
    
    public abstract class DependencyTrackerBase : IDependencyTracker
    {
        public virtual bool IsActive { get; protected set; }

        public Disposable<(DependencyTrackerBase, DependencyTrackerBase)> Activate()
        {
            var oldTracker = DependencyTracking.CurrentTracker;
            IsActive = true;
            DependencyTracking.CurrentTracker = this;
            return new Disposable<(DependencyTrackerBase, DependencyTrackerBase)>(
                state => {
                    var (current, old) = state;
                    DependencyTracking.CurrentTracker = old;
                    current.IsActive = false;
                }, (this, oldTracker));
        }
        
        public virtual void AddReactionToAllDependencies(Reaction reaction) => 
            // ReSharper disable once InconsistentNaming
            ApplyToAllDependencies(reaction, (_reaction, dependency) => dependency.AddReaction(_reaction));
        public virtual void RemoveReactionFromAllDependencies(Reaction reaction) =>
            // ReSharper disable once InconsistentNaming
            ApplyToAllDependencies(reaction, (_reaction, dependency) => dependency.RemoveReaction(_reaction));
        
        public abstract IEnumerable<ReactiveBase> Dependencies { get; }
        public abstract void RegisterDependency(ReactiveBase dependency);
        public abstract void ClearDependencies();
        // Generic virtual, so slow => must be avoided
        public abstract void ApplyToAllDependencies<TState>(TState state, Action<TState, ReactiveBase> action);
    }
    
    public class DependencyTracker : DependencyTrackerBase
    {
        private RefHashSetSlim4<ReactiveBase> _items = new RefHashSetSlim4<ReactiveBase>();
        
        // ReSharper disable once HeapView.BoxingAllocation
        public override IEnumerable<ReactiveBase> Dependencies => _items.Items;

        public override void RegisterDependency(ReactiveBase dependency) => _items.Add(dependency);
        public override void ClearDependencies() => _items.Clear();

        public override void ApplyToAllDependencies<TState>(TState state, Action<TState, ReactiveBase> action)
        {
            if (IsActive)
                throw Errors.DependencyTrackerIsActive();
            _items.Apply(state, action);
        }

        // Overriden for speed: the default one is relying on generic virtual ApplyToAllDependencies
        public override void AddReactionToAllDependencies(Reaction reaction)
        {
            if (IsActive)
                throw Errors.DependencyTrackerIsActive();
            // ReSharper disable once InconsistentNaming
            _items.Apply(reaction, (_reaction, dependency) => dependency.AddReaction(_reaction));
        }

        // Overriden for speed: the default one is relying on generic virtual ApplyToAllDependencies
        public override void RemoveReactionFromAllDependencies(Reaction reaction)
        {
            if (IsActive)
                throw Errors.DependencyTrackerIsActive();
            // ReSharper disable once InconsistentNaming
            _items.Apply(reaction, (_reaction, dependency) => dependency.RemoveReaction(_reaction));
        }
    }
}
