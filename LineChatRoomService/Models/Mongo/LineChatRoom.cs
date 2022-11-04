using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LineChatRoomService.Models.Mongo
{
    [BsonIgnoreExtraElements]
    public class LineChatRoom
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string? OwnerId { get; set; }

        public string? RoomName { get; set; }

        public string? RoomType { get; set; }

        public string? Token { get; set; }

        public DateTime CreateAt { get; set; } = DateTime.Now;

        public Subscriptions Subscriptions { get; set; } = new Subscriptions();
    }



}
