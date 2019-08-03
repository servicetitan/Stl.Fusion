using System;
using System.Collections.Generic;

namespace Stl.TimeSeries
{
    [Serializable]
    public readonly struct Point<T> : IEquatable<Point<T>>
    {
        public Time Time { get; }
        public T Value { get; }

        public Point(Time time, T value)
        {
            Time = time;
            Value = value;
        }
        public void Deconstruct(out Time time, out T value)
        {
            time = Time;
            value = Value;
        }
        public static implicit operator Point<T>((Time Time, T Value) source) => new Point<T>(source.Time, source.Value);
        public static implicit operator (Time Time, T Value) (Point<T> source) => (source.Time, source.Value);

        public override string ToString() => $"{Time}: {Value}";

        public bool Equals(Point<T> other) => Time.Equals(other.Time) && EqualityComparer<T>.Default.Equals(Value, other.Value);
        public override bool Equals(object? obj) => obj is Point<T> other && Equals(other);
        public override int GetHashCode() => unchecked(
            (Time.GetHashCode() * 397) ^ EqualityComparer<T>.Default.GetHashCode(Value));
        public static bool operator ==(Point<T> left, Point<T> right) => left.Equals(right);
        public static bool operator !=(Point<T> left, Point<T> right) => !left.Equals(right);
    }

    public static class Point
    {
        public static Point<T> New<T>(Time time, T value) => new Point<T>(time, value);
    }
}
