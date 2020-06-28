using Stl.Extensibility.Internal;

namespace Stl.Extensibility
{
    public interface ISpecial
    {
        object Service { get; }
    }

    public interface ISpecial<out TService> : ISpecial
        where TService : class
    {
        new TService Service { get; }
    }

    public class Special<TService, TFor> : ISpecial<TService>
        where TService : class
    {
        object ISpecial.Service => Service;
        public TService Service { get; }

        public Special(TService service) 
            => Service = service;
    }

    public static class Special
    {
        public static SpecialBuilder<TService> Use<TService>(TService service) 
            where TService : class
            => new SpecialBuilder<TService>(service);
    }
}
