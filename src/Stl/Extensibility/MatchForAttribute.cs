using System;
using Stl.Reflection;

namespace Stl.Extensibility
{
    [Serializable]
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Interface,
        AllowMultiple = true,
        Inherited = false)]
    public class MatchForAttribute : Attribute
    {
        public Type Source { get; }
        public string Scope { get; }

        public MatchForAttribute(Type source, Type? scope)
            : this(source, scope == null ? "" : scope.ToSymbol().Value) { }
        public MatchForAttribute(Type source, string scope = "")
        {
            Source = source;
            Scope = scope;
        }
    }
}
