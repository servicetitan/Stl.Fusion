using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Threading;
using Stl.Internal;

namespace Stl.ImmutableModel.Updating
{
    public interface IChangeTracker : IDisposable
    {
        IUpdater UntypedUpdater { get; }
        IObservable<UpdateInfo> this[DomainKey key, NodeChangeType changeTypeMask] { get; }
    }

    public interface IChangeTracker<TModel> : IChangeTracker
        where TModel : class, INode
    {
        IUpdater<TModel> Updater { get; }
        new IObservable<UpdateInfo<TModel>> this[DomainKey key, NodeChangeType changeTypeMask] { get; }
    }

    public sealed class ChangeTracker<TModel> : IChangeTracker<TModel>
        where TModel : class, INode
    {
        private ConcurrentDictionary<DomainKey, ImmutableDictionary<IObserver<UpdateInfo<TModel>>, NodeChangeType>>? _observers =
            new ConcurrentDictionary<DomainKey, ImmutableDictionary<IObserver<UpdateInfo<TModel>>, NodeChangeType>>();

        IUpdater IChangeTracker.UntypedUpdater => Updater;
        public IUpdater<TModel> Updater { get; }

        public ChangeTracker(IUpdater<TModel> updater)
        {
            Updater = updater;
            Updater.Updated += OnUpdated;
        }

        public void Dispose()
        {
            var observers = _observers;
            if (observers == null)
                return;
            _observers = null;
            Updater.Updated -= OnUpdated;
            foreach (var (_, d) in observers)
            foreach (var (o, _) in d)
                o.OnCompleted();
        }

        private void OnUpdated(UpdateInfo<TModel> updateInfo)
        {
            if (_observers == null)
                throw Errors.AlreadyDisposed();
            var changes = updateInfo.ChangeSet.Changes;
            foreach (var (key, changeType) in changes) {
                if (_observers.TryGetValue(key, out var d)) {
                    foreach (var (o, changeTypeMask) in d)
                        if ((changeTypeMask & changeType) != 0)
                            o.OnNext(updateInfo);
                }
            }
        }

        IObservable<UpdateInfo> IChangeTracker.this[DomainKey key, NodeChangeType changeTypeMask] => GetObservable(key, changeTypeMask);
        public IObservable<UpdateInfo<TModel>> this[DomainKey key, NodeChangeType changeTypeMask] => GetObservable(key, changeTypeMask);

        private IObservable<UpdateInfo<TModel>> GetObservable(DomainKey key, NodeChangeType changeTypeMask)
        {
            return Observable.Create<UpdateInfo<TModel>>(o => {
                _observers.AddOrUpdate(key, 
                    (p, state) => 
                        ImmutableDictionary<IObserver<UpdateInfo<TModel>>, NodeChangeType>.Empty
                            .Add(state.Observer, state.ChangeType),
                    (p, d, state) => 
                        d.Add(state.Observer, state.ChangeType),
                    (Self: this, Observer: o, ChangeType: changeTypeMask));
                // ReSharper disable once HeapView.BoxingAllocation
                return Disposable.New(state => {
                    var spinWait = new SpinWait();
                    while (true) {
                        if (_observers.TryGetValue(state.Key, out var d)) {
                            var dNew = d.Remove(state.Observer);
                            if (dNew.IsEmpty 
                                ? _observers.TryRemove(state.Key, d) // Notice it's an atomic update 
                                : _observers.TryUpdate(state.Key, dNew, d)) {
                                state.Observer.OnCompleted();
                                break;
                            }
                            spinWait.SpinOnce();
                        }
                        else
                            // No need to call OnCompleted, since someone else removed the observer
                            break;
                    }
                }, (Self: this, Key: key, Observer: o));
            });
        }
    }

    public static class ChangeTracker
    {
        public static ChangeTracker<TModel> New<TModel>(IUpdater<TModel> updater)
            where TModel : class, INode
            => new ChangeTracker<TModel>(updater);
    }
}
