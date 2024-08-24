using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DonkeyDog.Models
{
    public class Feedback
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public string Email { get; set; }
        public string Message { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)] 
        public DateTime DateCreated { get; set; }

        
    }


}
