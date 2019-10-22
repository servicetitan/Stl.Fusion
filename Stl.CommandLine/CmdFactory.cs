using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.CommandLine
{
    public interface ICmdFactory
    {
        bool IsInitialized { get; }
        TCmd New<TCmd>() where TCmd : ICmd;
        ICmd New(string cmdName);

        void Initialize();
        Task InitializeAsync();
    }
    
    public abstract class CmdFactoryBase : ICmdFactory
    {
        private volatile TaskCompletionSource<Unit>? _initializingTcs;
        private readonly Lazy<ImmutableDictionary<object, Func<ICmd>>> _lazyFactories;

        public bool IsInitialized => _initializingTcs?.Task.IsCompleted ?? false;
        protected ImmutableDictionary<object, Func<ICmd>> Factories => _lazyFactories.Value;

        protected CmdFactoryBase()
        {
            _lazyFactories = new Lazy<ImmutableDictionary<object, Func<ICmd>>>(() => {
                Initialize();
                var factories = new Dictionary<object, Func<ICmd>>();
                PopulateFactories(factories);
                return factories.ToImmutableDictionary();
            });
        }

        public virtual TCmd New<TCmd>()
            where TCmd : ICmd
        {
            var cmd = Factories[typeof(TCmd)].Invoke();
            ConfigureCmd(cmd);
            return (TCmd) cmd;
        }

        public virtual ICmd New(string cmdName)
        {
            var cmd = Factories[cmdName].Invoke();
            ConfigureCmd(cmd);
            return cmd;
        }

        protected virtual ICmd ConfigureCmd(ICmd cmd) => cmd;
        protected abstract Task InitializeImplAsync();
        protected abstract void PopulateFactories(Dictionary<object, Func<ICmd>> factories);

        public virtual void Initialize()
        {
            if (IsInitialized) {
                // Just to re-throw an exception, if any:
                _initializingTcs!.Task.Result.Ignore();
                return;
            }
            Task.Run(InitializeAsync).Wait();
        }

        public virtual async Task InitializeAsync()
        {
            if (IsInitialized) {
                // Just to re-throw an exception, if any:
                _initializingTcs!.Task.Result.Ignore();
                return;
            }

            var tcs = new TaskCompletionSource<Unit>();
            var oldTcs = Interlocked.CompareExchange(ref _initializingTcs, tcs, null);
            if (oldTcs != null) {
                oldTcs.Task.Result.Ignore();
                return;
            }

            try {
                await InitializeImplAsync().ConfigureAwait(false);
                tcs.SetResult(default);
            }
            catch (Exception e) {
                tcs.SetException(e);
                throw;
            }
        }

        protected static void AddFactory<TCmd>(
            Dictionary<object, Func<ICmd>> factories, 
            Func<TCmd> factory, 
            params string[] cmdNames)
            where TCmd : ICmd
        {
            factories[typeof(TCmd)] = () => (ICmd) factory.Invoke();
            foreach (var cmdName in cmdNames) 
                factories[cmdName] = () => (ICmd) factory.Invoke();
        }
    }
}
