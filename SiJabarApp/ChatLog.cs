using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SiJabarApp
{
    [BsonIgnoreExtraElements]
    public class ChatLog
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("role")]
        public string Role { get; set; } // Menyimpan "user" atau "bot"

        [BsonElement("message")]
        public string Message { get; set; }

        [BsonElement("timestamp")]
        public DateTime Timestamp { get; set; }
    }
}