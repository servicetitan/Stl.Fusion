using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Stl.Time
{
    [Serializable]
    public readonly struct Event<T> : IEquatable<Event<T>>, IHasHappenedAt
    {
        public T Value { get; }
        public Moment HappenedAt { get; }

        [JsonConstructor]
        public Event(T value, Moment happenedAt)
        {
            Value = value;
            HappenedAt = happenedAt;
        }

        public override string ToString() => $"{GetType().Name}({Value} @ {HappenedAt})";

        // Conversion
        
        public void Deconstruct(out T value, out Moment happenedAt)
        {
            value = Value;
            happenedAt = HappenedAt;
        }

        public static implicit operator Event<T>((T Value, Moment HappenedAt) source) => new Event<T>(source.Value, source.HappenedAt);
        public static implicit operator (T Value, Moment HappenedAt) (Event<T> source) => (source.Value, source.HappenedAt);

        // Equality
        
        public bool Equals(Event<T> other) => HappenedAt == other.HappenedAt && EqualityComparer<T>.Default.Equals(Value, other.Value);
        public override bool Equals(object? obj) => obj is Event<T> other && Equals(other);
        public override int GetHashCode() => unchecked(
            (HappenedAt.GetHashCode() * 397) ^ EqualityComparer<T>.Default.GetHashCode(Value));
        public static bool operator ==(Event<T> left, Event<T> right) => left.Equals(right);
        public static bool operator !=(Event<T> left, Event<T> right) => !left.Equals(right);
    }

    public static class Event
    {
        public static Event<T> New<T>(T value, Moment happenedAt) => new Event<T>(value, happenedAt);
    }
}
