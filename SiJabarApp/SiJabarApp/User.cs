using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SiJabarApp
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("Fullname")]
        public string Fullname { get; set; }

        [BsonElement("Email")]
        public string Email { get; set; }

        [BsonElement("Password")]
        public string Password { get; set; } // Ini nanti berisi password yang sudah di-hash (acak)
    }
}