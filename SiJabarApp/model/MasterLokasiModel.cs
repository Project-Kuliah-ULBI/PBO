using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SiJabarApp.model
{
    public class MasterLokasiModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string NamaTPS { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Keterangan { get; set; }
    }
}
