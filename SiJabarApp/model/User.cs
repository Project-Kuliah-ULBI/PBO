using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SiJabarApp.model
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
        public string Password { get; set; }

        // --- TAMBAHAN WAJIB (AGAR BISA MULTI-ROLE) ---
        [BsonElement("Role")]
        public string Role { get; set; }
    }
}