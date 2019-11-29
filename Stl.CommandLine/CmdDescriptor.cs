using System;
using System.Threading.Tasks;

namespace Stl.CommandLine 
{
    public class CmdDescriptor
    {
        public Type Type { get; }
        public Func<IServiceProvider, ICmd> Factory { get; private set; }
        public Func<IServiceProvider, Task> Initializer { get; private set; }
        public string Name { get; }
        public ReadOnlyMemory<string> Aliases { get; }

        public static CmdDescriptor New<TCmd>(
            Func<IServiceProvider, TCmd> factory, 
            string name, params string[] aliases)
            where TCmd : class, ICmd
            => new CmdDescriptor(typeof(TCmd), factory, c => Task.CompletedTask, name, aliases);

        public static CmdDescriptor New<TCmd>(
            Func<IServiceProvider, TCmd> factory,
            Func<IServiceProvider, Task> initializer,
            string name, params string[] aliases)
            where TCmd : class, ICmd
            => new CmdDescriptor(typeof(TCmd), factory, initializer, name, aliases);

        public CmdDescriptor(Type type, 
            Func<IServiceProvider, ICmd> factory, Func<IServiceProvider, Task> initializer, 
            string name, params string[] aliases)
        {
            Type = type;
            Factory = factory;
            Initializer = initializer;
            Name = name;
            Aliases = aliases;
        }

        public CmdDescriptor AddConfigurator(Func<ICmd, IServiceProvider, ICmd> configurator)
        {
            var clone = (CmdDescriptor) MemberwiseClone();
            clone.Factory = services => {
                var cmd = Factory.Invoke(services);
                cmd = configurator.Invoke(cmd, services);
                return cmd;
            }; 
            return clone;
        }

        public CmdDescriptor AddInitializer(Func<IServiceProvider, Task> initializer)
        {
            var clone = (CmdDescriptor) MemberwiseClone();
            var oldInitializer = Initializer;
            clone.Initializer = async services => {
                var oldInitializerTask = oldInitializer.Invoke(services);
                var initializerTask = initializer.Invoke(services);
                await Task.WhenAll(oldInitializerTask, initializerTask)
                    .ConfigureAwait(false);
            }; 
            return clone;
        }
    }
}
