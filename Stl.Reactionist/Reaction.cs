using System;
using System.Diagnostics;

namespace Stl.Reactionist
{
    [DebuggerDisplay(
        "State = {" + nameof(State) + "}, " +
        "Handler = {" + nameof(Handler) + "}")]
    public struct Reaction : IEquatable<Reaction>
    {
        public object State { get; }
        public Action<object, Event> Handler { get; }
        
        public Reaction(Action<object, Event> handler) : this(null, handler) { }
        public Reaction(object state, Action<object, Event> handler)
        {
            State = state;
            Handler = handler;
        }

        public override string ToString() => $"{GetType().Name}({State}, {Handler})";

        public void Invoke(Event @event) => Handler.Invoke(State, @event);

        public bool Equals(Reaction other) =>
            // Reference comparison; it's intentional here!
            State == other.State && Handler == other.Handler;
        public override bool Equals(object obj) => 
            !ReferenceEquals(null, obj) && (obj is Reaction other && Equals(other));
        public override int GetHashCode() => unchecked( 
            ((State != null ? State.GetHashCode() : 0) * 397) ^ 
            (Handler != null ? Handler.GetHashCode() : 0));

        public static bool operator ==(Reaction left, Reaction right) => left.Equals(right);
        public static bool operator !=(Reaction left, Reaction right) => !left.Equals(right);
    }
}
