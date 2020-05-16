using System;

namespace Stl.Fusion
{
    [Serializable]
    public readonly struct TaggedResult<T> : IEquatable<TaggedResult<T>>
    {
        public readonly Result<T> Result;
        public readonly int Tag;
        
        public TaggedResult(Result<T> result, int tag)
        {
            Result = result;
            Tag = tag;
        }

        public static implicit operator TaggedResult<T>((Result<T> Result, int Tag) source) 
            => new TaggedResult<T>(source.Result, source.Tag);

        public override string ToString() => $"{Result} (#{Tag})";

        public void Deconstruct(out Result<T> result, out int tag)
        {
            result = Result;
            tag = Tag;
        }

        // Equality

        public bool Equals(TaggedResult<T> other) 
            => Result == other.Result && Tag == other.Tag;
        public override bool Equals(object? obj)  
            => obj is TaggedResult<T> other && Equals(other);
        public override int GetHashCode() 
            => HashCode.Combine(Result, Tag);
        public static bool operator ==(TaggedResult<T> left, TaggedResult<T> right) 
            => left.Equals(right);
        public static bool operator !=(TaggedResult<T> left, TaggedResult<T> right) 
            => !left.Equals(right);
    }
}
