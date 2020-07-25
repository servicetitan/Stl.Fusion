using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Stl.Samples.Blazor.Common.Services
{
    public class LongKeyedEntity : IHasId<long>
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
    }

    public class ChatUser : LongKeyedEntity
    {
        [Required, MaxLength(120)]
        public string Name { get; set; } = "";
    }

    public class ChatMessage : LongKeyedEntity
    {
        public long UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        [Required, MaxLength(4000)]
        public string Text { get; set; } = "";
    }

    public class ChatPage
    {
        [JsonIgnore]
        public long? MinMessageId { get; }
        [JsonIgnore]
        public long? MaxMessageId { get; }
        // Must be sorted by ChatMessage.Id
        public List<ChatMessage> Messages { get; }
        public Dictionary<long, ChatUser> Users { get; }

        public ChatPage()
            : this(new List<ChatMessage>(), new Dictionary<long, ChatUser>()) { }
        [JsonConstructor]
        public ChatPage(List<ChatMessage> messages, Dictionary<long, ChatUser> users)
        {
            Messages = messages;
            Users = users;
            if (messages.Count > 0) {
                MinMessageId = messages.Min(m => m.Id);
                MaxMessageId = messages.Max(m => m.Id);
            }
        }
    }

    public interface IChatService
    {
        // Writers
        Task<ChatUser> CreateUserAsync(string name, CancellationToken cancellationToken = default);
        Task<ChatUser> SetUserNameAsync(long id, string name, CancellationToken cancellationToken = default);
        Task<ChatMessage> AddMessageAsync(long userId, string text, CancellationToken cancellationToken = default);

        // Readers
        Task<long> GetUserCountAsync(CancellationToken cancellationToken = default);
        Task<long> GetActiveUserCountAsync(CancellationToken cancellationToken = default);
        Task<ChatUser> GetUserAsync(long id, CancellationToken cancellationToken = default);
        Task<ChatPage> GetChatTailAsync(int length, CancellationToken cancellationToken = default);
        Task<ChatPage> GetChatPageAsync(long minMessageId, long maxMessageId, CancellationToken cancellationToken = default);
    }
}
