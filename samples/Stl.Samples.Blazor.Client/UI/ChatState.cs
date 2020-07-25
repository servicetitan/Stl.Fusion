using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Fusion.UI;
using Stl.Samples.Blazor.Client.Services;
using Stl.Samples.Blazor.Common.Services;

namespace Stl.Samples.Blazor.Client.UI
{
    public class ChatState
    {
        public long UserCount { get; set; } = 0;
        public long ActiveUserCount { get; set; } = 0;
        public ChatPage LastPage { get; set; } = new ChatPage();

        public class Local
        {
            // It's global to the app, so we store it in static field
            private static volatile ChatUser? _me;

            public ChatUser? Me {
                get => _me;
                set {
                    _me = value;
                    if (string.IsNullOrEmpty(MyName) && value != null)
                        MyName = value.Name;
                }
            }

            public string MyName { get; set; } = "";
            public string MyMessage { get; set; } = "";
            public Exception? Error { get; set; }

            public Local Clone()
                => (Local) MemberwiseClone();
        }

        public class Updater : ILiveStateUpdater<Local, ChatState>
        {
            protected IChatClient Client { get; }

            public Updater(IChatClient client) => Client = client;

            public virtual async Task<ChatState> UpdateAsync(
                ILiveState<Local, ChatState> liveState, CancellationToken cancellationToken)
            {
                var userCount = await Client.GetUserCountAsync(cancellationToken);
                var activeUserCount = await Client.GetActiveUserCountAsync(cancellationToken);
                var lastPage = await Client.GetChatTailAsync(30, cancellationToken);
                var state = new ChatState() {
                    UserCount = userCount,
                    ActiveUserCount = activeUserCount,
                    LastPage = lastPage,
                };
                return state;
            }
        }
    }

}
