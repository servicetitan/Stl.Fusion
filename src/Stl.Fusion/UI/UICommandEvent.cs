using System;
using System.Text.Json.Serialization;
using System.Threading;
using Stl.CommandR;
using Stl.Time;

namespace Stl.Fusion.UI
{
    [Serializable]
    public record UICommandEvent
    {
        private static long _nextCommandId;

        public long CommandId { get; init; } = Interlocked.Increment(ref _nextCommandId);
        public ICommand Command { get; init; } = null!;

        public Moment? CreatedAt { get; init; }
        public Moment? CompletedAt { get; init; }
        [JsonIgnore, Newtonsoft.Json.JsonIgnore]
        public TimeSpan? Duration => CompletedAt - CreatedAt;

        public IResult? Result { get; init; }
        [JsonIgnore, Newtonsoft.Json.JsonIgnore]
        public bool IsCompleted => Result != null;
        [JsonIgnore, Newtonsoft.Json.JsonIgnore]
        public bool IsCompletedSuccessfully => Result?.HasValue ?? false;
        [JsonIgnore, Newtonsoft.Json.JsonIgnore]
        public bool IsFailed => Result?.Error != null;

        public UICommandEvent() { }
        public UICommandEvent(ICommand command, IResult? result = null)
        {
            Command = command;
            Result = result;
        }
    }
}
