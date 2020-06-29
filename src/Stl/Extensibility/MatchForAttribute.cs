using System;

namespace Stl.Extensibility
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MatchForAttribute : Attribute
    {
        public Type Source { get; }
        public Type Scope { get; }

        public MatchForAttribute(Type source, Type scope)
        {
            Source = source;
            Scope = scope;
        }
    }
}
