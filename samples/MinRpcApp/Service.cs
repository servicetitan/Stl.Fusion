using System.Reactive;
using MemoryPack;
using Stl.CommandR;
using Stl.CommandR.Configuration;
using Stl.Fusion;

namespace Samples.MiniRpcApp;

public interface IChat : IComputeService
{
    [ComputeMethod]
    Task<List<string>> GetRecentMessages(CancellationToken cancellationToken = default);
    [ComputeMethod]
    Task<int> GetMessageCount(CancellationToken cancellationToken = default);

    [CommandHandler]
    Task Post(Chat_Post command, CancellationToken cancellationToken);
}

[MemoryPackable]
// ReSharper disable once InconsistentNaming
public partial record Chat_Post(string Message) : ICommand<Unit>;

public class Chat : IChat
{
    private readonly object _lock = new();
    private List<string> _posts = new();
    private int _postCount;

    public virtual Task<List<string>> GetRecentMessages(CancellationToken cancellationToken = default)
        => Task.FromResult(_posts);

    public virtual Task<int> GetMessageCount(CancellationToken cancellationToken = default)
        => Task.FromResult(_postCount);

    public virtual Task Post(Chat_Post command, CancellationToken cancellationToken)
    {
        if (Computed.IsInvalidating()) {
            _ = GetRecentMessages(default);
            _ = GetMessageCount(default);
            return Task.CompletedTask;
        }

        lock (_lock) {
            var posts = _posts.ToList(); // We can't update the list itself (it's shared), but can re-create it
            posts.Add(command.Message);
            if (posts.Count > 10)
                posts.RemoveAt(0);
            _posts = posts;
            _postCount++;
        }
        return Task.CompletedTask;
    }
}
