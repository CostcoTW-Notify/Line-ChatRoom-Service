using LineChatRoomService.Models.Mongo;
using LineChatRoomService.Repositories.Interface;
using MongoDB.Driver;

namespace LineChatRoomService.Repositories
{
    public class ChatRoomRepository : IChatRoomRepository
    {

        public ChatRoomRepository(IMongoDatabase mongoDB)
        {
            this.ChatRoomCollection = mongoDB.GetCollection<LineChatRoom>("ChatRooms");
        }

        public IMongoCollection<LineChatRoom> ChatRoomCollection { get; }

        public Task<LineChatRoom> Create(LineChatRoom chatRoom)
        {
            chatRoom.Id = null;
            this.ChatRoomCollection.InsertOne(chatRoom);
            return Task.FromResult(chatRoom);
        }

        public Task Delete(LineChatRoom chatRoom)
            => this.ChatRoomCollection.DeleteOneAsync(x => x.Id == chatRoom.Id);

        public Task<LineChatRoom?> GetById(string id)
            => this.ChatRoomCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

        public Task<List<LineChatRoom>> GetByOwner(string ownerId)
            => this.ChatRoomCollection.Find(x => x.OwnerId == ownerId).ToListAsync();

        public async Task<LineChatRoom> Update(LineChatRoom chatRoom)
        {
            if (string.IsNullOrWhiteSpace(chatRoom.Id))
                throw new ArgumentException("Must provide Id field.");

            await this.ChatRoomCollection.ReplaceOneAsync(x => x.Id == chatRoom.Id, chatRoom);
            return chatRoom;
        }
    }
}
