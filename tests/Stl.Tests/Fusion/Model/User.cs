using System.ComponentModel.DataAnnotations;

namespace Stl.Tests.Fusion.Model
{
    public class User : LongKeyedEntity
    {
        private string _name = "";
        private string _email = "";

        [Required, MaxLength(120)]
        public string Name {
            get => _name;
            set { ThrowIfFrozen(); _name = value; }
        }

        [Required, MaxLength(250)]
        public string Email {
            get => _email;
            set { ThrowIfFrozen(); _email = value; }
        }
    }
}
