using System;
using Newtonsoft.Json;
using Stl.Internal;

namespace Stl.Frozen
{
    public interface IFrozen
    {
        bool IsFrozen { get; }
        void Freeze(); // Must freeze every reachable IFrozen too!

        IFrozen CloneToUnfrozenUntyped(bool deep = false);
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

        public virtual IFrozen CloneToUnfrozenUntyped(bool deep = false)
        {
            var clone = (FrozenBase) MemberwiseClone();
            clone.IsFrozen = false;
            return clone;
        }

        // Protected

        protected void ThrowIfFrozen()
        {
            if (IsFrozen) throw Errors.MustBeUnfrozen();
        }

        protected void ThrowIfNotFrozen()
        {
            if (!IsFrozen) throw Errors.MustBeFrozen();
        }
    }
}
