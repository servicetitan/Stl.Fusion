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
            set => _name = PreparePropertyValue(nameof(Name), value);
        }

        [Required, MaxLength(250)]
        public string Email {
            get => _email;
            set => _name = PreparePropertyValue(nameof(Email), value);
        }
    }
}
