using System;
using System.ComponentModel.DataAnnotations;

namespace Stl.Tests.Fusion.Model
{
    public class Message : LongKeyedEntity
    {
        private string _text = "";
        private DateTime _date;
        private User _author = default!;
        private Chat _chat = default!;

        public DateTime Date {
            get => _date;
            set => _date = PreparePropertyValue(nameof(Date), value);
        }

        [Required, MaxLength(1_000_000)]
        public string Text {
            get => _text;
            set => _text = PreparePropertyValue(nameof(Text), value);
        }

        [Required]
        public User Author {
            get => _author;
            set => _author = PreparePropertyValue(nameof(Author), value);
        }

        [Required]
        public Chat Chat {
            get => _chat;
            set => _chat = PreparePropertyValue(nameof(Chat), value);
        }
    }
}
