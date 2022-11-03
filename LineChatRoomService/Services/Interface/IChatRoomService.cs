using LineChatRoomService.Models.Mongo;

namespace LineChatRoomService.Services.Interface
{
    public interface IChatRoomService
    {

        Task<bool> SendMessageToChatRoom(string roomId, string testMessage);

        Task RevokeChatRoom(string roomId);

        Task CreateChatRoom(string ownedId, string token);

        Task UpdateChatRoom(LineChatRoom charRoom);

    }
}
