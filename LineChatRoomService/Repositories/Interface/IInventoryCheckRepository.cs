using LineChatRoomService.Models.Mongo;

namespace LineChatRoomService.Repositories.Interface
{
    public interface IInventoryCheckRepository
    {

        Task<InventoryCheckItem?> GetByItemCode(string code);

        Task CreateNewCheckItem(InventoryCheckItem item);

        Task UpdateCheckItem(InventoryCheckItem item);

        Task DeleteCheckItemByCode(string code);
    }
}
