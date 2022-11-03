using LineChatRoomService.Models.Mongo;
using LineChatRoomService.Repositories.Interface;
using LineChatRoomService.Services.Interface;

namespace LineChatRoomService.Services
{
    public class ChatRoomService : IChatRoomService
    {

        public ChatRoomService(
            IHttpContextAccessor httpContextAccessor,
            ILineNotifyService service,
            IChatRoomRepository repo)
        {
            this.ChatRoomRepo = repo;
            this.LineNotifyService = service;
            this.UserId = httpContextAccessor.HttpContext!.User.Claims
                                             .Where(x => x.Type == "sub")
                                             .FirstOrDefault()?.Value;
        }

        public IChatRoomRepository ChatRoomRepo { get; }
        public ILineNotifyService LineNotifyService { get; }
        public string? UserId { get; }

        public async Task CreateChatRoom(string ownerdId, string token)
        {
            var info = await LineNotifyService.GetChatRoomInfomation(token);

            if (info is null || string.IsNullOrWhiteSpace(info.targetType) || string.IsNullOrWhiteSpace(info.target))
                throw new Exception("Cannot get chat room information");

            var chatRoom = new LineChatRoom
            {
                OwnerId = ownerdId,
                Token = token,
                RoomName = info.target,
                RoomType = info.targetType,
            };

            var newChatRoom = await ChatRoomRepo.Create(chatRoom);
            Console.WriteLine($"User: {ownerdId} create new chat room: {newChatRoom.Id}");
        }

        public async Task RevokeChatRoom(string roomId)
        {
            if (string.IsNullOrWhiteSpace(UserId))
                throw new ArgumentException(nameof(UserId));

            var chatRoom = await this.ChatRoomRepo.GetById(roomId);

            if (chatRoom is null)
                throw new ArgumentException(nameof(roomId));

            if (chatRoom.OwnerId != UserId)
                throw new Exception("Access Denied");

            var token = chatRoom.Token;
            await this.ChatRoomRepo.Delete(chatRoom);
            await this.LineNotifyService.RevokeChatRoom(token!);
        }

        public async Task<bool> SendMessageToChatRoom(string roomId, string testMessage)
        {
            if (string.IsNullOrWhiteSpace(UserId))
                throw new ArgumentException(nameof(UserId));

            var chatRoom = await this.ChatRoomRepo.GetById(roomId);

            if (chatRoom is null)
                throw new ArgumentException(nameof(roomId));

            if (chatRoom.OwnerId != UserId)
                throw new Exception("Access Denied");

            var token = chatRoom.Token;
            var result = await this.LineNotifyService.SendMessage(token, testMessage);
            return result;
        }

        public Task UpdateChatRoom(LineChatRoom charRoom)
        {
            throw new NotImplementedException();
        }
    }
}
