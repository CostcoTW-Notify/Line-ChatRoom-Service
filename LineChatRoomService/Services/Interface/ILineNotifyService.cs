using LineChatRoomService.Models;

namespace LineChatRoomService.Services.Interface
{
    public interface ILineNotifyService
    {
        string BuildChallengeUrl(string redirectUri, string user);

        Task<string?> ExchangeCodeAsync(string code);

        (string redirectUrl, string user) GetInfoFromState(string state);

        Task<ChatRoomInformation?> GetChatRoomInfomation(string token);

        Task<bool> SendMessage(string token, string testMessage);

        Task RevokeChatRoom(string token);

    }
}
