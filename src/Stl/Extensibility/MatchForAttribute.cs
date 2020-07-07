using System;
using Stl.Reflection;

namespace Stl.Extensibility
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MatchForAttribute : Attribute
    {
        public Type Source { get; }
        public string Scope { get; }

        public MatchForAttribute(Type source, string scope)
        {
            Source = source;
            Scope = scope;
        }

        public MatchForAttribute(Type source, Type scope)
        {
            Source = source;
            Scope = scope.ToSymbol();
        }
    }
}
