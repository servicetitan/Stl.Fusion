using System.ComponentModel.DataAnnotations;

namespace Stl.Tests.Fusion.Model
{
    public class Chat : LongKeyedEntity
    {
        private string _title = "";
        private User _author = default!;

        [Required, MaxLength(120)]
        public string Title {
            get => _title;
            set { ThrowIfFrozen(); _title = value; }
        }

        [Required]
        public User Author {
            get => _author;
            set { ThrowIfFrozen(); _author = value; }
        }
    }
}
