using Stl.CommandR;

namespace Stl.Fusion.Authentication
{
    public interface ISessionCommand
    {
        Session Session { get; init; }
    }

    public interface ISessionCommand<TResult> : ICommand<TResult>, ISessionCommand
    { }
}
