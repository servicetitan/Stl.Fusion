using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.CommandLine.Git;
using Stl.CommandLine.Python;

namespace Stl.CommandLine
{
    public interface ICmdFactory : IEnumerable<CmdDescriptor>
    {
        TCmd New<TCmd>() where TCmd : ICmd;
        ICmd New(string cmdName);
    }
    
    public abstract class CmdFactoryBase : ICmdFactory
    {
        private readonly Lazy<ImmutableList<CmdDescriptor>> _lazyCommands;
        private readonly Lazy<ImmutableDictionary<object, CmdDescriptor>> _lazyCommandByKey;
        private readonly ILogger _log;

        protected IServiceProvider Services { get; }
        protected ImmutableList<CmdDescriptor> Commands => _lazyCommands.Value;
        protected ImmutableDictionary<object, CmdDescriptor> CommandByKey => _lazyCommandByKey.Value;

        protected CmdFactoryBase(
            IServiceProvider services, 
            ILogger<CmdFactoryBase>? log = null)
        {
            _log = log ??= NullLogger<CmdFactoryBase>.Instance;
            Services = services;
            _lazyCommands = new Lazy<ImmutableList<CmdDescriptor>>(() => {
                var commands = new List<CmdDescriptor>();
                PopulateCommands(commands);
                InitializeCommands(commands);
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

        public virtual TCmd New<TCmd>()
            where TCmd : ICmd 
            => (TCmd) CommandByKey[typeof(TCmd)].Factory.Invoke(Services);

        public virtual ICmd New(string cmdName) 
            => CommandByKey[cmdName].Factory.Invoke(Services);

        // Protected methods

        protected virtual void PopulateCommands(List<CmdDescriptor> commands)
        {
            commands.AddRange(new [] {
                CmdDescriptor.New(_ => new ShellCmd(), "shell", "sh"),
                CmdDescriptor.New(_ => new GitCmd(), "git"),
                CmdDescriptor.New(_ => new Python2Cmd(), "python2", "py2"),
                CmdDescriptor.New(_ => new Python3Cmd(), "python3", "py3"),
            });
        }

        protected virtual void InitializeCommands(List<CmdDescriptor> commands)
        {
            var tasks = new List<Task>();
            for (var i = 0; i < commands.Count; i++) {
                var cmd = commands[i];
                cmd = cmd.AddConfigurator((cmd1, _) => Configure(cmd1));
                commands[i] = cmd;
                tasks.Add(Task.Run(() => cmd.Initializer.Invoke(Services)));
            }
            Task.WaitAll(tasks.ToArray());
        }

        protected virtual ICmd Configure(ICmd cmd) => cmd;
    }
}
