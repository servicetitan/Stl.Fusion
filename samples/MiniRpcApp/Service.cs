using System.Reactive;
using System.Runtime.Serialization;
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
    Task<int> GetWordCount(CancellationToken cancellationToken = default);

    [CommandHandler]
    Task Post(Chat_Post command, CancellationToken cancellationToken);
}

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
// ReSharper disable once InconsistentNaming
public partial record Chat_Post(
    [property: DataMember, MemoryPackOrder(0)] string Message
    ) : ICommand<Unit>;

public class Chat : IChat
{
    private readonly object _lock = new();
    private List<string> _posts = new();

    public virtual Task<List<string>> GetRecentMessages(CancellationToken cancellationToken = default)
        => Task.FromResult(_posts);

    public virtual async Task<int> GetWordCount(CancellationToken cancellationToken = default)
    {
        // Note that GetRecentMessages call here becomes a dependency of WordCount call,
        // and that's why it gets invalidated automatically.
        var messages = await GetRecentMessages(cancellationToken).ConfigureAwait(false);
        return messages
            .Select(m => m.Split(" ", StringSplitOptions.RemoveEmptyEntries).Length)
            .Sum();
    }

    public virtual Task Post(Chat_Post command, CancellationToken cancellationToken)
    {
        if (Computed.IsInvalidating()) {
            _ = GetRecentMessages(default); // No need to invalidate GetWordCount
            return Task.CompletedTask;
        }

        lock (_lock) {
            var posts = _posts.ToList(); // We can't update the list itself (it's shared), but can re-create it
            posts.Add(command.Message);
            if (posts.Count > 10)
                posts.RemoveAt(0);
            _posts = posts;
        }
        return Task.CompletedTask;
    }
}
