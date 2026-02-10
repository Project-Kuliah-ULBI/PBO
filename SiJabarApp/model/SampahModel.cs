using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SiJabarApp.model
{
    public class SampahModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string UserId { get; set; }
        public string Wilayah { get; set; }
        public string Jenis { get; set; }
        public double Berat { get; set; }
        public string Status { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime Tanggal { get; set; }
        public DateTime JadwalAngkut { get; set; }
        public string Keterangan { get; set; }
    }
}
