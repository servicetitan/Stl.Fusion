using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stl.Fusion.Bridge;
using Stl.Fusion.Server;
using Stl.Samples.Blazor.Common.Services;

namespace Stl.Samples.Blazor.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : FusionController, IChatService
    {
        private readonly IChatService _chat;

        public ChatController(IChatService chat, IPublisher publisher)
            : base(publisher)
            => _chat = chat;

        // Writers

        [HttpPost("createUser")]
        public Task<ChatUser> CreateUserAsync(string? name, CancellationToken cancellationToken = default)
        {
            name ??= "";
            return _chat.CreateUserAsync(name, cancellationToken);
        }

        [HttpPost("setUserName")]
        public async Task<ChatUser> SetUserNameAsync(long id, string? name, CancellationToken cancellationToken = default)
        {
            name ??= "";
            return await _chat.SetUserNameAsync(id, name, cancellationToken);
        }

        [HttpPost("addMessage")]
        public async Task<ChatMessage> AddMessageAsync(long userId, string? text, CancellationToken cancellationToken = default)
        {
            text ??= "";
            return await _chat.AddMessageAsync(userId, text, cancellationToken);
        }

        // Readers

        [HttpGet("getUserCount")]
        public Task<long> GetUserCountAsync(CancellationToken cancellationToken = default)
            => PublishAsync(ct => _chat.GetUserCountAsync(ct));

        [HttpGet("getActiveUserCount")]
        public Task<long> GetActiveUserCountAsync(CancellationToken cancellationToken = default)
            => PublishAsync(ct => _chat.GetActiveUserCountAsync(ct));

        [HttpGet("getUser")]
        public Task<ChatUser> GetUserAsync(long id, CancellationToken cancellationToken = default)
            => PublishAsync(ct => _chat.GetUserAsync(id, ct));

        [HttpGet("getChatTail")]
        public Task<ChatPage> GetChatTailAsync(int length, CancellationToken cancellationToken = default)
            => PublishAsync(ct => _chat.GetChatTailAsync(length, ct));

        [HttpGet("getChatPage")]
        public Task<ChatPage> GetChatPageAsync(long minMessageId, long maxMessageId, CancellationToken cancellationToken = default)
            => PublishAsync(ct => _chat.GetChatPageAsync(minMessageId, maxMessageId, ct));
    }
}
