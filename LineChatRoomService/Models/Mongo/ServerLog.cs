using MongoDB.Bson.Serialization.Attributes;

namespace LineChatRoomService.Models.Mongo
{
    [BsonIgnoreExtraElements]
    public class ServerLog
    {

        public DateTime StartTime { get; set; }

        /// <summary>
        /// unit : ms
        /// </summary>
        public uint ProcessTime { get; set; }

        public Request Request { get; set; }

        public int ResponseStatus { get; set; }

        public string? Error { get; set; }

    }

    [BsonIgnoreExtraElements]
    public class Request
    {
        public string Method { get; set; }

        public string Url { get; set; }

        public string? Body { get; set; }

    }
}
