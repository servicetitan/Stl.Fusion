using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using Stl.Internal;

namespace Stl.ImmutableModel.Updating
{
    public interface IChangeTracker : IDisposable
    {
        IObservable<UpdateInfo> UntypedAllChanges { get; }
        IObservable<UpdateInfo> UntypedChangesIncluding(DomainKey key, NodeChangeType changeTypeMask);
    }

    public interface IChangeTracker<TModel> : IChangeTracker
        where TModel : class, INode
    {
        IObservable<UpdateInfo<TModel>> AllChanges { get; }
        IObservable<UpdateInfo<TModel>> ChangesIncluding(DomainKey key, NodeChangeType changeTypeMask);
    }

    public sealed class ChangeTracker<TModel> : IChangeTracker<TModel>
        where TModel : class, INode
    {
        private bool _isDisposed;
        private readonly IUpdater<TModel> _updater;
        private readonly Subject<UpdateInfo<TModel>> _allChanges;
        private readonly ConcurrentDictionary<DomainKey, ImmutableDictionary<IObserver<UpdateInfo<TModel>>, NodeChangeType>> _observers =
            new ConcurrentDictionary<DomainKey, ImmutableDictionary<IObserver<UpdateInfo<TModel>>, NodeChangeType>>();

        IObservable<UpdateInfo> IChangeTracker.UntypedAllChanges => _allChanges;
        public IObservable<UpdateInfo<TModel>> AllChanges => _allChanges;

        public ChangeTracker(IUpdater<TModel> updater)
        {
            _updater = updater;
            _updater.Updated += OnUpdated;
            _allChanges = new Subject<UpdateInfo<TModel>>();
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;
            _isDisposed = true;
            _updater.Updated -= OnUpdated;
            _allChanges.OnCompleted();
            foreach (var (_, d) in _observers)
            foreach (var (o, _) in d)
                o.OnCompleted();
            _allChanges.Dispose();
        }

        private void OnUpdated(UpdateInfo<TModel> updateInfo)
        {
            if (_observers == null)
                throw Errors.AlreadyDisposed();
            _allChanges.OnNext(updateInfo);
            var changes = updateInfo.ChangeSet.Changes;
            foreach (var (key, changeType) in changes) {
                if (_observers.TryGetValue(key, out var d)) {
                    foreach (var (o, changeTypeMask) in d)
                        if ((changeTypeMask & changeType) != 0)
                            o.OnNext(updateInfo);
                }
            }
        }

        IObservable<UpdateInfo> IChangeTracker.UntypedChangesIncluding(DomainKey key, NodeChangeType changeTypeMask) => ChangesIncluding(key, changeTypeMask);
        public IObservable<UpdateInfo<TModel>> ChangesIncluding(DomainKey key, NodeChangeType changeTypeMask)
        {
            if (_observers == null)
                throw Errors.AlreadyDisposed();
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
