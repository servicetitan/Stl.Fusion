using Stl.CommandR;

namespace Stl.Fusion.Authentication
{
    public interface ISessionCommand : ICommand
    {
        Session Session { get; init; }
    }

    public interface ISessionCommand<TResult> : ICommand<TResult>, ISessionCommand
    { }
}
