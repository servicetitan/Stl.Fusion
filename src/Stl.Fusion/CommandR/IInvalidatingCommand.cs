using Stl.CommandR;

namespace Stl.Fusion.CommandR
{
    // Tagging interface for commands that require invalidation pass
    public interface IInvalidatingCommand : ICommand { }
    public interface IInvalidatingCommand<TResult> : ICommand<TResult>, IInvalidatingCommand { }

    // Tagging interface for commands that require invalidation pass even on failure
    public interface IAlwaysInvalidatingCommand : IInvalidatingCommand { }
    public interface IAlwaysInvalidatingCommand<TResult> : IInvalidatingCommand<TResult>, IAlwaysInvalidatingCommand { }
}
