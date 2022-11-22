using LineChatRoomService.Models.Mongo;

namespace LineChatRoomService.Repositories.Interface
{
    public interface IServerLogRepository
    {

        Task InsertLog(ServerLog log);
    }
}
