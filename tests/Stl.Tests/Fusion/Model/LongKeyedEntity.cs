using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Stl.ImmutableModel;

namespace Stl.Tests.Fusion.Model
{
    public class LongKeyedEntity : Node<LongKey>, IHasKey<LongKey>
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Id {
            get => Key.Value;
            set => Key = new LongKey(value);
        }
    }
}
