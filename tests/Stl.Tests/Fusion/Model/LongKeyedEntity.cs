using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Stl.Frozen;

namespace Stl.Tests.Fusion.Model
{
    public class LongKeyedEntity : FrozenBase, IHasId<long>
    {
        private long _id;

        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Id {
            get => _id;
            set { ThrowIfFrozen(); _id = value; }
        }
    }
}
