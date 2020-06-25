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
    public class ChatController : Controller, IChatService
    {
        protected IChatService ChatService { get; }
        protected IPublisher Publisher { get; }

        public ChatController(
            IChatService chatService,
            IPublisher publisher)
        {
            ChatService = chatService;
            Publisher = publisher;
        }

        // Writers

        [HttpPost("createUser")]
        public Task<ChatUser> CreateUserAsync(string? name, CancellationToken cancellationToken = default)
        {
            name ??= "";
            return ChatService.CreateUserAsync(name, cancellationToken);
        }

        [HttpPost("setUserName")]
        public async Task<ChatUser> SetUserNameAsync(long id, string? name, CancellationToken cancellationToken = default)
        {
            name ??= "";
            return await ChatService.SetUserNameAsync(id, name, cancellationToken);
        }

        [HttpPost("addMessage")]
        public async Task<ChatMessage> AddMessageAsync(long userId, string? text, CancellationToken cancellationToken = default)
        {
            text ??= "";
            return await ChatService.AddMessageAsync(userId, text, cancellationToken);
        }

        // Readers

        [HttpGet("getUserCount")]
        public async Task<long> GetUserCountAsync(CancellationToken cancellationToken = default)
        {
            var c = await HttpContext.TryPublishAsync(Publisher, 
                _ => ChatService.GetUserCountAsync(cancellationToken),
                cancellationToken);
            return c.Value;
        }

        [HttpGet("getActiveUserCount")]
        public async Task<long> GetActiveUserCountAsync(CancellationToken cancellationToken = default)
        {
            var c = await HttpContext.TryPublishAsync(Publisher, 
                _ => ChatService.GetActiveUserCountAsync(cancellationToken),
                cancellationToken);
            return c.Value;
        }

        [HttpGet("getUser")]
        public async Task<ChatUser> GetUserAsync(long id, CancellationToken cancellationToken = default)
        {
            var c = await HttpContext.TryPublishAsync(Publisher, 
                _ => ChatService.GetUserAsync(id, cancellationToken),
                cancellationToken);
            return c.Value;
        }

        [HttpGet("getChatTail")]
        public async Task<ChatPage> GetChatTailAsync(int length, CancellationToken cancellationToken = default)
        {
            var c = await HttpContext.TryPublishAsync(Publisher, 
                _ => ChatService.GetChatTailAsync(length, cancellationToken),
                cancellationToken);
            return c.Value;
        }

        [HttpGet("getChatPage")]
        public async Task<ChatPage> GetChatPageAsync(long minMessageId, long maxMessageId, CancellationToken cancellationToken = default)
        {
            var c = await HttpContext.TryPublishAsync(Publisher, 
                _ => ChatService.GetChatPageAsync(minMessageId, maxMessageId, cancellationToken),
                cancellationToken);
            return c.Value;
        }
    }
}
