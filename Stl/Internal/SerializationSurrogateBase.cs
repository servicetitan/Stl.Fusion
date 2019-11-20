using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Stl.Internal
{
    // Helps to deserialize types that don't support serialization by
    // substituting them with a type (the descendant of this one) that does.
    [Serializable]
    public abstract class SerializationSurrogateBase<TActual, TOwner>
    {
        [JsonIgnore]
        [field: NonSerialized]
        public TOwner Owner { [return: MaybeNull] get; set; } = default!;

        [JsonIgnore]
        [field: NonSerialized]
        public Action<TOwner, TActual>? OwnerPropertySetter { get; set; } = null;
        
        [OnDeserialized]
        protected virtual void OnDeserialized() 
            => OwnerPropertySetter?.Invoke(Owner, ToActualObject());

        protected abstract TActual ToActualObject();
    }
}