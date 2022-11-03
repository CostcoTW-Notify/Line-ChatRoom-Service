using LineChatRoomService.Models.Mongo;

namespace LineChatRoomService.Repositories.Interface
{
    public interface IChatRoomRepository
    {

        Task<LineChatRoom> GetById(string id);

        Task<List<LineChatRoom>> GetByOwner(string ownerId);

        Task<LineChatRoom> Create(LineChatRoom chatRoom);

        Task<LineChatRoom> Update(LineChatRoom chatRoom);

        Task Delete(LineChatRoom chatRoom);
    }
}
