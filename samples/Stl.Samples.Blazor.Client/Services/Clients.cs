using System;
using System.Threading;
using System.Threading.Tasks;
using RestEase;
using Stl.Fusion.Bridge;
using Stl.Fusion.Client;
using Stl.Samples.Blazor.Common.Services;

namespace Stl.Samples.Blazor.Client.Services
{
    [Header(FusionHeaders.RequestPublication, "1")]
    public interface ITimeClient : IReplicaService
    {
        [Get("get")]
        Task<DateTime> GetTimeAsync(CancellationToken cancellationToken = default);
    }

    [Header(FusionHeaders.RequestPublication, "1")]
    public interface IChatClient : IReplicaService
    {
        // Writers
        [Post("createUser"), ReplicaServiceMethod(false)]
        Task<ChatUser> CreateUserAsync(string name, CancellationToken cancellationToken = default);
        [Post("setUserName"), ReplicaServiceMethod(false)]
        Task<ChatUser> SetUserNameAsync(long id, string name, CancellationToken cancellationToken = default);
        [Post("addMessage"), ReplicaServiceMethod(false)]
        Task<ChatMessage> AddMessageAsync(long userId, string text, CancellationToken cancellationToken = default);

        // Readers
        [Get("getUserCount")]
        Task<long> GetUserCountAsync(CancellationToken cancellationToken = default);
        [Get("getActiveUserCount")]
        Task<long> GetActiveUserCountAsync(CancellationToken cancellationToken = default);
        [Get("getUser")]
        Task<ChatUser> GetUserAsync(long id, CancellationToken cancellationToken = default);
        [Get("getChatTail")]
        Task<ChatPage> GetChatTailAsync(int length, CancellationToken cancellationToken = default);
        [Get("getChatPage")]
        Task<ChatPage> GetChatPageAsync(long minMessageId, long maxMessageId, CancellationToken cancellationToken = default);
    }

    [Header(FusionHeaders.RequestPublication, "1")]
    public interface IScreenshotClient : IReplicaService
    {
        [Get("get")]
        Task<Screenshot> GetScreenshotAsync(int width, CancellationToken cancellationToken = default);
    }

    [Header(FusionHeaders.RequestPublication, "1")]
    public interface IComposerClient : IReplicaService
    {
        [Get("get")]
        Task<ComposedValue> GetComposedValueAsync(string? parameter, CancellationToken cancellationToken = default);
    }
}
