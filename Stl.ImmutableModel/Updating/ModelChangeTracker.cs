using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using Stl.Internal;

namespace Stl.ImmutableModel.Updating
{
    public interface IModelChangeTracker : IDisposable
    {
        IObservable<IModelUpdateInfo> AllChanges { get; }
        IObservable<IModelUpdateInfo> ChangesIncluding(Key key, NodeChangeType changeTypeMask);
    }

    public interface IModelChangeTracker<TModel> : IModelChangeTracker
        where TModel : class, INode
    {
        new IObservable<ModelUpdateInfo<TModel>> AllChanges { get; }
        new IObservable<ModelUpdateInfo<TModel>> ChangesIncluding(Key key, NodeChangeType changeTypeMask);
    }

    public interface IModelChangeNotify
    {
        void OnModelUpdated(IModelUpdateInfo updateInfo);
    }

    public interface IModelChangeNotify<TModel> : IModelChangeNotify
        where TModel : class, INode
    {
        void OnModelUpdated(ModelUpdateInfo<TModel> updateInfo);
    }

    public sealed class ModelChangeTracker<TModel> : IModelChangeTracker<TModel>, IModelChangeNotify<TModel>
        where TModel : class, INode
    {
        private bool _isDisposed;
        private readonly Subject<ModelUpdateInfo<TModel>> _allChanges =
            new Subject<ModelUpdateInfo<TModel>>();
        private readonly ConcurrentDictionary<Key, ImmutableDictionary<IObserver<ModelUpdateInfo<TModel>>, NodeChangeType>> _observers =
            new ConcurrentDictionary<Key, ImmutableDictionary<IObserver<ModelUpdateInfo<TModel>>, NodeChangeType>>();

        IObservable<IModelUpdateInfo> IModelChangeTracker.AllChanges => _allChanges;
        public IObservable<ModelUpdateInfo<TModel>> AllChanges => _allChanges;

        public void Dispose()
        {
            if (_isDisposed)
                return;
            _isDisposed = true;
            _allChanges.OnCompleted();
            foreach (var (_, d) in _observers)
            foreach (var (o, _) in d)
                o.OnCompleted();
            _allChanges.Dispose();
        }

        void IModelChangeNotify.OnModelUpdated(IModelUpdateInfo updateInfo) 
            => OnModelUpdated((ModelUpdateInfo<TModel>) updateInfo);
        public void OnModelUpdated(ModelUpdateInfo<TModel> updateInfo)
        {
            if (_observers == null)
                throw Errors.AlreadyDisposed();
            _allChanges.OnNext(updateInfo);
            var changes = updateInfo.ChangeSet;
            foreach (var (key, changeType) in changes) {
                if (_observers.TryGetValue(key, out var d)) {
                    foreach (var (o, changeTypeMask) in d)
                        if ((changeTypeMask & changeType) != 0)
                            o.OnNext(updateInfo);
                }
            }
        }

        IObservable<IModelUpdateInfo> IModelChangeTracker.ChangesIncluding(Key key, NodeChangeType changeTypeMask) => ChangesIncluding(key, changeTypeMask);
        public IObservable<ModelUpdateInfo<TModel>> ChangesIncluding(Key key, NodeChangeType changeTypeMask)
        {
            if (_observers == null)
                throw Errors.AlreadyDisposed();
            return Observable.Create<ModelUpdateInfo<TModel>>(o => {
                _observers.AddOrUpdate(key, 
                    (p, state) => 
                        ImmutableDictionary<IObserver<ModelUpdateInfo<TModel>>, NodeChangeType>.Empty
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
}
