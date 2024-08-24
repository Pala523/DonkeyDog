using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace DonkeyDog.Models
{
    public class Servizi
    {
        public Guid Id { get; set; } 
        public required string Title { get; set; }
        public required string Description { get; set; }
        public required string Icon { get; set; }

    }
}
