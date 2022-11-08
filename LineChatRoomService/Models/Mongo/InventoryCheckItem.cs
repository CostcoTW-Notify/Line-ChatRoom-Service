using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LineChatRoomService.Models.Mongo
{
    [BsonIgnoreExtraElements]
    public class InventoryCheckItem
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string Code { get; set; }

        public string Name { get; set; }

        public string[] SubscriptionChatRoom { get; set; }

    }
}
