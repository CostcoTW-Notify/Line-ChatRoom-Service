using LineChatRoomService.Models.Mongo;
using LineChatRoomService.Repositories.Interface;
using MongoDB.Driver;

namespace LineChatRoomService.Repositories
{
    public class InventoryCheckRepository : IInventoryCheckRepository
    {
        public IMongoCollection<InventoryCheckItem> InventoryCheckCollection { get; }

        public InventoryCheckRepository(IMongoDatabase mongoDB)
        {
            this.InventoryCheckCollection = mongoDB.GetCollection<InventoryCheckItem>("InventoryCheck");
        }

        public Task<InventoryCheckItem?> GetByItemCode(string code)
            => this.InventoryCheckCollection.Find(x => x.Code == code).FirstOrDefaultAsync();

        public Task CreateNewCheckItem(InventoryCheckItem item)
        {
            item.Id = null;
            this.InventoryCheckCollection.InsertOne(item);
            return Task.CompletedTask;
        }

        public async Task UpdateCheckItem(InventoryCheckItem item)
        {
            if (string.IsNullOrWhiteSpace(item.Id))
                throw new ArgumentException("Must provide Id field.");

            await this.InventoryCheckCollection.ReplaceOneAsync(x => x.Id == item.Id, item);
        }

        public Task DeleteCheckItemByCode(string code)
        {
            this.InventoryCheckCollection.DeleteMany(x => x.Code == code);
            return Task.CompletedTask;
        }
    }
}
