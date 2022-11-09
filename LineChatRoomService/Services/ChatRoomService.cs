using LineChatRoomService.Extensions;
using LineChatRoomService.Extensions.ModelMapper;
using LineChatRoomService.Models;
using LineChatRoomService.Models.Mongo;
using LineChatRoomService.Repositories.Interface;
using LineChatRoomService.Services.Interface;
using System.Net.Http.Json;
using System.Text;

namespace LineChatRoomService.Services
{
    public class ChatRoomService : IChatRoomService
    {

        private readonly ILogger<ChatRoomService> logger;

        public IChatRoomRepository ChatRoomRepo { get; }
        public IInventoryCheckRepository InventoryCheckRepo { get; }
        public ILineNotifyService LineNotifyService { get; }

        public readonly HttpContext? HttpContext;

        public IHttpClientFactory HttpClientFactory { get; }

        public string? UserId { get => this.HttpContext?.GetUserId(); }


        public ChatRoomService(
            ILogger<ChatRoomService> logger,
            IHttpContextAccessor httpContextAccessor,
            IHttpClientFactory httpClientFactory,
            ILineNotifyService service,
            IChatRoomRepository chatRoomRepo,
            IInventoryCheckRepository inventoryCheckRepo)
        {
            this.logger = logger;
            this.ChatRoomRepo = chatRoomRepo;
            this.InventoryCheckRepo = inventoryCheckRepo;
            this.LineNotifyService = service;
            this.HttpContext = httpContextAccessor.HttpContext;
            this.HttpClientFactory = httpClientFactory;
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
            await this.UpdateInventoryCheckItems(chatRoom!.Id!, new string[] { });
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

            var subsItem = chatRooms.SelectMany(x => x.Subscriptions.InventoryCheckList);

            var itemNames = new Dictionary<string, string>();

            foreach (var code in subsItem)
            {
                var item = await this.InventoryCheckRepo.GetByItemCode(code);
                if (item is null)
                    itemNames[code] = "unknown";
                else
                    itemNames[code] = item.Name;

            }

            var viewModels = chatRooms.Select(x => x.ToChatRoomViewModel()).ToList();

            viewModels.ForEach(r =>
            {
                if (r.Subscriptions?.InventoryCheckList is null)
                    return;

                foreach (var code in r.Subscriptions.InventoryCheckList.Keys)
                {
                    r.Subscriptions.InventoryCheckList[code] = itemNames[code];
                }
            });

            return viewModels;
        }

        public async Task<ChatRoomViewModel?> GetChatRoomById(string roomId)
        {
            var chatRoom = await this.ChatRoomRepo.GetById(roomId);

            EnsureChatRoomExists(chatRoom);

            var subsItem = chatRoom!.Subscriptions.InventoryCheckList;

            var itemNames = new Dictionary<string, string>();

            foreach (var code in subsItem)
            {
                var item = await this.InventoryCheckRepo.GetByItemCode(code);
                if (item is null)
                    itemNames[code] = "unknown";
                else
                    itemNames[code] = item.Name;

            }

            var viewModel = chatRoom!.ToChatRoomViewModel();

            if (viewModel.Subscriptions?.InventoryCheckList is not null)
                foreach (var code in viewModel.Subscriptions.InventoryCheckList.Keys)
                {
                    viewModel.Subscriptions.InventoryCheckList[code] = itemNames[code];
                }

            return viewModel;
        }

        public async Task UpdateChatRoom(ChatRoomViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Id))
                throw new ArgumentException(nameof(model));

            var chatRoom = await this.ChatRoomRepo.GetById(model.Id!);

            EnsureChatRoomExists(chatRoom);


            if (model.Subscriptions is null)
                throw new ArgumentNullException(nameof(model.Subscriptions));

            if (model.Subscriptions.DailyNewOnSale is not null)
                chatRoom!.Subscriptions.DailyNewOnSale = model.Subscriptions.DailyNewOnSale.Value;

            if (model.Subscriptions.DailyNewBestBuy is not null)
                chatRoom!.Subscriptions.DailyNewBestBuy = model.Subscriptions.DailyNewBestBuy.Value;

            if (model.Subscriptions.InventoryCheckList is not null)
            {
                chatRoom!.Subscriptions.InventoryCheckList = model.Subscriptions.InventoryCheckList.Keys.ToList();
                await UpdateInventoryCheckItems(chatRoom.Id!, chatRoom.Subscriptions.InventoryCheckList);
            }

            await this.ChatRoomRepo.Update(chatRoom!);

            var sb = new StringBuilder();
            sb.Append(" 商品監測設定已變更如下\n");
            // 2705 : V  40DA : X
            sb.Append($"新特價商品通知:  " + (chatRoom!.Subscriptions.DailyNewOnSale ? "\u2714" : "\u2718") + "\n");
            sb.Append($"新最低價商品通知:  " + (chatRoom!.Subscriptions.DailyNewBestBuy ? "\u2714" : "\u2718") + "\n");
            sb.Append($"庫存監控商品: \n");
            chatRoom.Subscriptions.InventoryCheckList.ForEach(x =>
            {
                sb.Append($"#{x}\n");
            });
            await this.SendMessageToChatRoom(chatRoom.Id!, sb.ToString());
        }


        private async Task UpdateInventoryCheckItems(string chatRoomId, IEnumerable<string> items)
        {
            var chatRoom = await this.ChatRoomRepo.GetById(chatRoomId);
            if (chatRoom is null)
                throw new Exception("ChatRoom not exists");
            var current = chatRoom.Subscriptions.InventoryCheckList;

            var unsubscriptionItems = current.Except(items);

            var newSubscriptionItems = items.Except(current);

            foreach (var code in unsubscriptionItems)
            {
                var checkItem = await this.InventoryCheckRepo.GetByItemCode(code);

                if (checkItem is null)
                    continue;

                if (checkItem.SubscriptionChatRoom.Contains(chatRoomId))
                {
                    var newSubsChatRooms = checkItem.SubscriptionChatRoom.ToHashSet();
                    newSubsChatRooms.Remove(chatRoomId);
                    checkItem.SubscriptionChatRoom = newSubsChatRooms.ToArray();

                    if (newSubsChatRooms.Count == 0)
                        await this.InventoryCheckRepo.DeleteCheckItemByCode(checkItem.Code);
                    else
                        await this.InventoryCheckRepo.UpdateCheckItem(checkItem);
                }

            }

            var client = this.HttpClientFactory.CreateClient("default");

            foreach (var code in newSubscriptionItems)
            {
                var checkItem = await this.InventoryCheckRepo.GetByItemCode(code);
                if (checkItem is null)
                {
                    var result = await client.GetFromJsonAsync<CostcoProductInformation>($"https://www.costco.com.tw/rest/v2/taiwan/metadata/productDetails?code={code}");
                    //var req = new HttpRequestMessage(HttpMethod.Get, $"https://www.costco.com.tw/rest/v2/taiwan/metadata/productDetails?code={code}");

                    //var response = await client.SendAsync(req);

                    //if (!response.IsSuccessStatusCode)
                    //    continue;

                    //var result = await response.Content.ReadFromJsonAsync<CostcoProductInformation>();

                    if (result is null || string.IsNullOrWhiteSpace(result.MetaTitle))
                        continue;

                    var newItem = new InventoryCheckItem
                    {
                        Code = code,
                        Name = result.MetaTitle,
                        SubscriptionChatRoom = new[] { chatRoomId }
                    };

                    await this.InventoryCheckRepo.CreateNewCheckItem(newItem);
                }
                else
                {
                    var newSubsChatRooms = checkItem.SubscriptionChatRoom.ToHashSet();
                    newSubsChatRooms.Add(chatRoomId);
                    checkItem.SubscriptionChatRoom = newSubsChatRooms.ToArray();
                    await this.InventoryCheckRepo.UpdateCheckItem(checkItem);
                }
            }

        }

        private void EnsureChatRoomExists(LineChatRoom? chatRoom)
        {
            if (chatRoom is null || chatRoom.OwnerId != this.UserId)
                throw new Exception("the specific room id is not exists");
        }
    }
}
