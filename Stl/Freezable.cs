using System;
using Newtonsoft.Json;

namespace Stl
{
    public interface IFreezable 
    {
        bool IsFrozen { get; }
        void Freeze(); // Must freeze every reachable IFreezable too!

        IFreezable BaseDefrost();
    }

    public abstract class FreezableBase: IFreezable
    {
        [JsonIgnore]
        [field: NonSerialized]
        public bool IsFrozen { get; private set; }
        
        public virtual void Freeze()
        {
            // The implementors of this method should
            // recursively freeze the children first!
            IsFrozen = true;
        }

        public virtual IFreezable BaseDefrost()
        {
            var clone = (FreezableBase) MemberwiseClone();
            clone.IsFrozen = false;
            return clone;
        }
    }
}
