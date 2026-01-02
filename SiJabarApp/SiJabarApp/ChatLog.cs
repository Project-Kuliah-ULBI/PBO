using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SiJabarApp
{
    public class ChatLog
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string Role { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public string ModelUsed { get; set; } // Properti baru ditambahkan di sini
    }
}