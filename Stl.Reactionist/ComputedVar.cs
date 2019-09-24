using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Stl.Reactionist
{
    public interface IComputedVar : IReadOnlyVar
    {
        bool IsAutoComputed { get; }
        bool IsComputed { get; }
        void Invalidate();
    }
    public interface IComputedVar<T> : IReadOnlyVar<T>, IComputedVar { }

    [DebuggerDisplay("({" + nameof(InternalResult) + "}, IsComputed = {" + nameof(IsComputed) + "})")]
    public class ComputedVar<T> : ReactiveWithReactionsBase, IComputedVar<T>
    {
        protected Func<T> Compute { get; }
        protected DependencyTrackerBase DependencyTracker { get; }
        protected Result<T> InternalResult { get; set; }
        Result<T> IHasInternalResult<T>.InternalResult => InternalResult;

        public bool IsAutoComputed { get; protected set; } // Protected setter enables auto compute delay in .ctor
        public bool IsComputed { get; protected set; }

        public Result<T> Result {
            get {
                RegisterDependency();
                EnsureComputed();
                TriggerReactions(new Event(this, ChangedEventData.Instance));
                return InternalResult;
            }
        }

        public Exception? Error => Result.Error;
        public bool HasError => Error != null;
        public T Value => Result.Value;
        public T UnsafeValue => Result.UnsafeValue;

        // ReSharper disable once HeapView.BoxingAllocation
        object? IResult.Value => Result.Value;
        // ReSharper disable once HeapView.BoxingAllocation
        object? IResult.UnsafeValue => Result.UnsafeValue;

        public ComputedVar(Func<T> compute, bool isAutoComputed = true, DependencyTrackerBase? dependencyTracker = null)
        {
            Compute = compute;
            IsAutoComputed = isAutoComputed;
            DependencyTracker = dependencyTracker ?? new DependencyTracker();
            if (isAutoComputed)
                // ReSharper disable once VirtualMemberCallInConstructor
                EnsureComputed();
        }

        public override string? ToString() => Value?.ToString();

        public virtual void Invalidate()
        {
            if (!IsComputed)
                return;
            IsComputed = false;
            DependencyTracker.RemoveReactionFromAllDependencies(new Reaction(this, ComputedVar.InvalidateAction));
            if (IsAutoComputed)
                EnsureComputed();
        }

        public void Deconstruct(out T value, out Exception? error) 
            => Result.Deconstruct(out value, out error);

        public void ThrowIfError() => Result.ThrowIfError();

        // Operators

        public static implicit operator T(ComputedVar<T> v) => v.Value;
        
        // Protected & private members

        protected virtual void EnsureComputed() 
        {
            if (IsComputed)
                return;
            Result<T> result;
            var dependencyTracker = DependencyTracker;
            var deactivator = dependencyTracker.Activate();
            try {
                result = Compute();
            }
            catch (Exception error) {
                result = (default, error);
            }
            finally {
                deactivator.Dispose();
            }
            dependencyTracker.AddReactionToAllDependencies(
                new Reaction(this, ComputedVar.InvalidateAction));
            IsComputed = true;
            if (EqualityComparer<Result<T>>.Default.Equals(InternalResult, result))
                return;
            InternalResult = result;
            TriggerReactions(new Event(this, ChangedEventData.Instance));
        }
    }

    public static class ComputedVar
    {
        internal static readonly Action<object?, Event> InvalidateAction = 
            (state, @event) => ((IComputedVar) state!).Invalidate();

        public static ComputedVar<T> New<T>(
            Func<T> compute, bool isAutoComputed = true, DependencyTrackerBase? dependencyTracker = null) => 
            new ComputedVar<T>(compute, isAutoComputed, dependencyTracker);
    }
}
