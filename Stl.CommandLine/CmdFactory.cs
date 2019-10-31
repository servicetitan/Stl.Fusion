using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Stl.CommandLine.Git;
using Stl.CommandLine.Python;

namespace Stl.CommandLine
{
    public class CmdDescriptor
    {
        public Type Type { get; }
        public Func<ICmd> Factory { get; private set; }
        public string Name { get; }
        public ReadOnlyMemory<string> Aliases { get; }

        public static CmdDescriptor New<TCmd>(Func<TCmd> factory, string name, params string[] aliases)
            where TCmd : class, ICmd
            => new CmdDescriptor(typeof(TCmd), factory, name, aliases);

        public CmdDescriptor(Type type, Func<ICmd> factory, string name, params string[] aliases)
        {
            Type = type;
            Name = name;
            Aliases = aliases;
            Factory = factory;
        }

        public CmdDescriptor AddConfigurator(Func<ICmd, ICmd> configurator)
        {
            var clone = (CmdDescriptor) MemberwiseClone();
            clone.Factory = () => {
                var cmd = Factory.Invoke();
                cmd = configurator.Invoke(cmd);
                return cmd;
            }; 
            return clone;
        }
    }

    public interface ICmdFactory : IEnumerable<CmdDescriptor>
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
        private readonly Lazy<ImmutableList<CmdDescriptor>> _lazyCommands;
        private readonly Lazy<ImmutableDictionary<object, CmdDescriptor>> _lazyCommandByKey;

        public bool IsInitialized => _initializingTcs?.Task.IsCompleted ?? false;
        protected ImmutableList<CmdDescriptor> Commands => _lazyCommands.Value;
        protected ImmutableDictionary<object, CmdDescriptor> CommandByKey => _lazyCommandByKey.Value;

        protected CmdFactoryBase()
        {
            _lazyCommands = new Lazy<ImmutableList<CmdDescriptor>>(() => {
                Initialize();
                var commands = new List<CmdDescriptor>();
                PopulateCommands(commands);
                return commands.ToImmutableList();
            });
            _lazyCommandByKey = new Lazy<ImmutableDictionary<object, CmdDescriptor>>(() => {
                var d = new Dictionary<object, CmdDescriptor>();
                foreach (var cmdDescriptor in Commands) {
                    d[cmdDescriptor.Type] = cmdDescriptor;
                    d[cmdDescriptor.Name] = cmdDescriptor;
                    foreach (var alias in cmdDescriptor.Aliases.Span)
                        d[alias] = cmdDescriptor;
                }
                return d.ToImmutableDictionary();
            });
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<CmdDescriptor> GetEnumerator() => Commands.GetEnumerator();

        protected virtual void PopulateCommands(List<CmdDescriptor> commands)
        {
            // Just a shortcut
            void Add(CmdDescriptor cmdDescriptor) 
                => commands.Add(cmdDescriptor.AddConfigurator(ConfigureCmd));

            Add(CmdDescriptor.New(() => new ShellCmd(), "shell", "sh"));
            Add(CmdDescriptor.New(() => new GitCmd(), "git"));
            Add(CmdDescriptor.New(() => new Python2Cmd(), "python2", "py2"));
            Add(CmdDescriptor.New(() => new Python3Cmd(), "python3", "py3"));
        }

        public virtual TCmd New<TCmd>()
            where TCmd : ICmd 
            => (TCmd) CommandByKey[typeof(TCmd)].Factory.Invoke();

        public virtual ICmd New(string cmdName) 
            => CommandByKey[cmdName].Factory.Invoke();

        protected virtual ICmd ConfigureCmd(ICmd cmd) => cmd;

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

        protected abstract Task InitializeImplAsync();
    }
}
