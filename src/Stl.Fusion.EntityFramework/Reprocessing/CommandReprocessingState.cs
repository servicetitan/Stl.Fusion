using System;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Stl.Generators;
using Stl.Serialization;

namespace Stl.Fusion.EntityFramework.Reprocessing
{
    public record CommandReprocessingState
    {
        private record PrettyPrint
        {
            public int MaxAttemptCount { get; init; }
            public int FailureCount { get; init; }
            public ErrorInfo? Error { get; init; }
            public CommandReprocessingDecision? Decision { get; init; }
        }

        public static Generator<long> Rng = new RandomInt32Generator();

        public int MaxAttemptCount { get; init; } = 3;
        public int FailureCount { get; init; }
        public Exception? Error { get; init; }
        public CommandReprocessingDecision Decision { get; init; } = new(true);

        public virtual CommandReprocessingDecision ComputeDecision()
        {
            if (FailureCount >= MaxAttemptCount)
                return new(false);
            if (Error is not DbUpdateException)
                return new(false);
            var delay = TimeSpan.FromMilliseconds(10 + Math.Abs(Rng.Next() % 100));
            return new(true, delay);
        }

        protected virtual bool PrintMembers(StringBuilder builder)
        {
            var pp = new PrettyPrint() {
                MaxAttemptCount = MaxAttemptCount,
                FailureCount = FailureCount,
                Error = Error.ToErrorInfo(),
                Decision = Decision,
            };
            var ppFormat = pp.ToString();
            var membersStartIndex = ppFormat.IndexOf('{');
            var membersEndIndex = ppFormat.LastIndexOf('}');
            var ppMembers = ppFormat.Substring(membersStartIndex + 2, membersEndIndex - membersStartIndex - 5);
            builder.Append(ppMembers);
            return true;
        }
    }
}
