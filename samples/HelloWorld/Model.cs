using System.Collections.Immutable;
using System.Text;
using Stl.Collections;

namespace Samples.HelloWorld
{
    public record Project
    {
        public string Id { get; init; } = "";
        public ImmutableList<string> DependsOn { get; init; } = ImmutableList<string>.Empty;

        public Project() { }
        public Project(string id, params string[] dependsOn)
        {
            Id = id;
            DependsOn = dependsOn.ToImmutableList();
        }

        protected virtual bool PrintMembers(StringBuilder builder)
        {
            builder.AppendFormat($"Id: {Id}, DependsOn: [{DependsOn.ToDelimitedString()}]");
            return true;
        }
    }

    public record ProjectBuildResult
    {
        public Project Project { get; init; } = null!;
        public long Version { get; init; }
        public string Artifacts { get; init; } = "";
    }
}