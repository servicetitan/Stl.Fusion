using System;

namespace Stl.Reactionist
{
    public struct Event : IEquatable<Event>
    {
        public object Source { get; }
        public object Data { get; }

        public Event(object source, object data)
        {
            Source = source;
            Data = data;
        }

        public override string ToString() => $"{GetType().Name}({Source}, {Data})";

        public bool Equals(Event other) => 
            Source == other.Source && Data == other.Data;
        public override bool Equals(object obj) => 
            obj != null && (obj is Event other && Equals(other));
        public override int GetHashCode() => unchecked(
            ((Source?.GetHashCode() ?? 0) * 397) ^ (Data?.GetHashCode() ?? 0));

        public static bool operator ==(Event left, Event right) => left.Equals(right);
        public static bool operator !=(Event left, Event right) => !left.Equals(right);
        
        public void Deconstruct<TSource, TData>(out TSource source, out TData data)
        {
            source = (TSource) Source;
            data = (TData) Data;
        }
    }
    
    // Interfaces -- they should be used for event type checks
    
    public interface IChangedEventData { }
    public interface IDisposedEventData { }
   
    // Implementations -- typically singletons, though some might have per-event instances

    public abstract class EventData
    {
        public override string ToString() => GetType().Name;
    }

    public class ChangedEventData : EventData, IChangedEventData
    {
        public static ChangedEventData Instance { get; } = new ChangedEventData();

        // ReSharper disable once MemberCanBePrivate.Global -- can be called from descendants
        protected ChangedEventData() { }
    }
}
