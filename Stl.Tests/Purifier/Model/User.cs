using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Stl.ImmutableModel;

namespace Stl.Tests.Purifier.Model
{
    public class User : Node<LongKey>, IHasKey<LongKey>
    {
        private string _name = "";
        private string _email = "";

        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Id {
            get => Key.Value;
            set => Key = new LongKey(value);
        }

        [Required, MaxLength(120)]
        public string Name {
            get => _name;
            set { this.ThrowIfFrozen(); _name = value; }
        }

        [Required, MaxLength(250)]
        public string Email {
            get => _email;
            set { this.ThrowIfFrozen(); _email = value; }
        }
    }
}
