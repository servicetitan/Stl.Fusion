using System;

namespace Stl.ImmutableModel
{
    [Serializable]
    [AttributeUsage(AttributeTargets.Property)]
    public class NodePropertyAttribute : Attribute
    {
        public bool IsNodeProperty { get; set; } = true;
    }
}
