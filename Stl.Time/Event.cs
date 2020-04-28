using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Stl.Time
{
    [Serializable]
    public readonly struct Event<T> : IEquatable<Event<T>>
    {
        public T Value { get; }
        public Moment Moment { get; }

        [JsonConstructor]
        public Event(T value, Moment moment)
        {
            Value = value;
            Moment = moment;
        }

        // Conversion
        
        public override string ToString() => $"({Value} @ {Moment})";

        public void Deconstruct(out T value, out Moment moment)
        {
            value = Value;
            moment = Moment;
        }

        public static implicit operator Event<T>((T Value, Moment Moment) source) => new Event<T>(source.Value, source.Moment);
        public static implicit operator (T Value, Moment Moment) (Event<T> source) => (source.Value, source.Moment);

        // Equality
        
        public bool Equals(Event<T> other) => Moment == other.Moment && EqualityComparer<T>.Default.Equals(Value, other.Value);
        public override bool Equals(object? obj) => obj is Event<T> other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Moment, Value);
        public static bool operator ==(Event<T> left, Event<T> right) => left.Equals(right);
        public static bool operator !=(Event<T> left, Event<T> right) => !left.Equals(right);
    }

    public static class Event
    {
        public static Event<T> New<T>(T value, Moment happenedAt) => new Event<T>(value, happenedAt);
    }
}
