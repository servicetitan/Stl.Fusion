using System;
using System.ComponentModel.DataAnnotations;

namespace Stl.Fusion.Tests.Model
{
    public class Message : LongKeyedEntity
    {
        private string _text = "";
        private DateTime _date;
        private User _author = default!;
        private Chat _chat = default!;

        public DateTime Date {
            get => _date;
            set { ThrowIfFrozen(); _date = value; }
        }

        [Required, MaxLength(1_000_000)]
        public string Text {
            get => _text;
            set { ThrowIfFrozen(); _text = value; }
        }

        [Required]
        public User Author {
            get => _author;
            set { ThrowIfFrozen(); _author = value; }
        }

        [Required]
        public Chat Chat {
            get => _chat;
            set { ThrowIfFrozen(); _chat = value; }
        }
    }
}
