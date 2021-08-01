using System;
using System.Reactive;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Stl.CommandR;

namespace Stl.Fusion.UI
{
    public class LocalCommand : ICommand<Unit>
    {
        public string Title { get; init; } = "Local command";
        [JsonIgnore, Newtonsoft.Json.JsonIgnore]
        public Func<Task>? Handler { get; init; }

        public LocalCommand(Action handler)
            => Handler = () => {
                handler.Invoke();
                return Task.CompletedTask;
            };

        public LocalCommand(Func<Task> handler)
            => Handler = handler;

        public LocalCommand(string title, Action handler)
        {
            Title = title;
            Handler = () => {
                handler.Invoke();
                return Task.CompletedTask;
            };
        }

        public LocalCommand(string title, Func<Task> handler)
        {
            Title = title;
            Handler = handler;
        }
    }
}
