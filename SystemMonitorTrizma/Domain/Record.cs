using System;

namespace Domain
{
    public class Record
    {
        public Hardware Hardware { get; set; }
        public int Value { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
