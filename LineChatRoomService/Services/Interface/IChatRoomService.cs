using LineChatRoomService.Models;
using LineChatRoomService.Models.Mongo;

namespace LineChatRoomService.Services.Interface
{
    public interface IChatRoomService
    {

        Task<bool> SendMessageToChatRoom(string roomId, string testMessage);

        Task RevokeChatRoom(string roomId);

        Task CreateChatRoom(string ownedId, string token);

        Task UpdateChatRoom(ChatRoomViewModel charRoom);

        Task<IEnumerable<ChatRoomViewModel>> GetAllChatRooms();

        Task<ChatRoomViewModel?> GetChatRoomById(string roomId);

    }
}
