using LineChatRoomService.Models.Mongo;
using LineChatRoomService.Repositories.Interface;
using MongoDB.Driver;

namespace LineChatRoomService.Repositories
{
    public class ServerLogRepository : IServerLogRepository
    {
        public IMongoCollection<ServerLog> ServerLogCollection { get; }


        public ServerLogRepository(IMongoDatabase mongoDB)
        {
            this.ServerLogCollection = mongoDB.GetCollection<ServerLog>("ServerLog");
        }


        public Task InsertLog(ServerLog log)
            => this.ServerLogCollection.InsertOneAsync(log);
    }
}
