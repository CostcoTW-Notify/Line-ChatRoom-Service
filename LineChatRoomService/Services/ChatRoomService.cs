using LineChatRoomService.Extensions;
using LineChatRoomService.Extensions.ModelMapper;
using LineChatRoomService.Models;
using LineChatRoomService.Models.Mongo;
using LineChatRoomService.Repositories.Interface;
using LineChatRoomService.Services.Interface;

namespace LineChatRoomService.Services
{
    public class ChatRoomService : IChatRoomService
    {

        private readonly ILogger<ChatRoomService> logger;

        public IChatRoomRepository ChatRoomRepo { get; }

        public ILineNotifyService LineNotifyService { get; }

        public readonly HttpContext? HttpContext;

        public string? UserId { get => this.HttpContext?.GetUserId(); }


        public ChatRoomService(
            ILogger<ChatRoomService> logger,
            IHttpContextAccessor httpContextAccessor,
            ILineNotifyService service,
            IChatRoomRepository repo)
        {
            this.logger = logger;
            this.ChatRoomRepo = repo;
            this.LineNotifyService = service;
            this.HttpContext = httpContextAccessor.HttpContext;
        }



        public async Task CreateChatRoom(string ownerdId, string token)
        {
            var info = await LineNotifyService.GetChatRoomInfomation(token);

            if (info is null || string.IsNullOrWhiteSpace(info.targetType) || string.IsNullOrWhiteSpace(info.target))
            {
                await LineNotifyService.RevokeChatRoom(token);
                throw new Exception("Cannot get chat room information... try revoke token...");
            }

            var chatRoom = new LineChatRoom
            {
                OwnerId = ownerdId,
                Token = token,
                RoomName = info.target,
                RoomType = info.targetType,
            };

            var newChatRoom = await ChatRoomRepo.Create(chatRoom);
            logger.LogInformation($"User: {ownerdId} create new chat room: {newChatRoom.Id}");
        }

        public async Task RevokeChatRoom(string roomId)
        {
            if (string.IsNullOrWhiteSpace(UserId))
                throw new ArgumentException(nameof(UserId));

            var chatRoom = await this.ChatRoomRepo.GetById(roomId);

            EnsureChatRoomExists(chatRoom);

            var token = chatRoom!.Token;
            await this.ChatRoomRepo.Delete(chatRoom);
            await this.LineNotifyService.RevokeChatRoom(token!);
        }

        public async Task<bool> SendMessageToChatRoom(string roomId, string testMessage)
        {
            if (string.IsNullOrWhiteSpace(UserId))
                throw new ArgumentException(nameof(UserId));

            var chatRoom = await this.ChatRoomRepo.GetById(roomId);

            EnsureChatRoomExists(chatRoom);

            var token = chatRoom!.Token;
            var result = await this.LineNotifyService.SendMessage(token, testMessage);
            return result;
        }


        public async Task<IEnumerable<ChatRoomViewModel>> GetAllChatRooms()
        {
            var chatRooms = await this.ChatRoomRepo.GetByOwner(this.UserId!);

            var viewModels = chatRooms.Select(x => x.ToChatRoomViewModel());

            return viewModels;
        }

        public async Task<ChatRoomViewModel?> GetChatRoomById(string roomId)
        {
            var chatRoom = await this.ChatRoomRepo.GetById(roomId);

            EnsureChatRoomExists(chatRoom);

            var viewModel = chatRoom!.ToChatRoomViewModel();
            return viewModel;
        }

        public async Task UpdateChatRoom(ChatRoomViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Id))
                throw new ArgumentException(nameof(model));

            var chatRoom = await this.ChatRoomRepo.GetById(model.Id!);

            EnsureChatRoomExists(chatRoom);

            var subs = model.Subscriptions;

            if (subs is null)
                throw new ArgumentNullException(nameof(model.Subscriptions));

            if (subs.DailyNewOnSale is not null)
                chatRoom.Subscriptions.DailyNewOnSale = subs.DailyNewOnSale.Value;

            if (subs.DailyNewBestBuy is not null)
                chatRoom.Subscriptions.DailyNewBestBuy = subs.DailyNewBestBuy.Value;

            if (subs.InventoryCheckList is not null)
                chatRoom.Subscriptions.InventoryCheckList = subs.InventoryCheckList;

            await this.ChatRoomRepo.Update(chatRoom);

        }

        private void EnsureChatRoomExists(LineChatRoom? chatRoom)
        {
            if (chatRoom is null || chatRoom.OwnerId != this.UserId)
                throw new Exception("the specific room id is not exists");
        }
    }
}
