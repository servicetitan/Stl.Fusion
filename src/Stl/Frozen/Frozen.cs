using System;
using Newtonsoft.Json;

namespace Stl.Frozen
{
    public interface IFrozen 
    {
        bool IsFrozen { get; }
        void Freeze(); // Must freeze every reachable IFrozen too!

        IFrozen BaseToUnfrozen(bool deep = false);
    }

    [Serializable]
    public abstract class FrozenBase: IFrozen
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

        public virtual IFrozen BaseToUnfrozen(bool deep = false)
        {
            var clone = (FrozenBase) MemberwiseClone();
            clone.IsFrozen = false;
            return clone;
        }
    }
}
