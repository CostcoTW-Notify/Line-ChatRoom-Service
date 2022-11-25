using LineChatRoomService.Extensions;
using LineChatRoomService.Extensions.ModelMapper;
using LineChatRoomService.Models;
using LineChatRoomService.Models.Microservice;
using LineChatRoomService.Models.Mongo;
using LineChatRoomService.Repositories.Interface;
using LineChatRoomService.Services.Interface;
using MongoDB.Bson;
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

        public ISubscriptionService SubscriptionService { get; }

        public string? UserId { get => this.HttpContext?.GetUserId(); }


        public ChatRoomService(
            ILogger<ChatRoomService> logger,
            IHttpContextAccessor httpContextAccessor,
            IHttpClientFactory httpClientFactory,
            ILineNotifyService service,
            IChatRoomRepository chatRoomRepo,
            IInventoryCheckRepository inventoryCheckRepo,
            ISubscriptionService subscriptionService)
        {
            this.logger = logger;
            this.ChatRoomRepo = chatRoomRepo;
            this.InventoryCheckRepo = inventoryCheckRepo;
            this.LineNotifyService = service;
            this.HttpContext = httpContextAccessor.HttpContext;
            this.HttpClientFactory = httpClientFactory;
            this.SubscriptionService = subscriptionService;
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
            await this.SubscriptionService.DeleteAllSubscription(token!);
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
            {
                chatRoom!.Subscriptions.DailyNewOnSale = model.Subscriptions.DailyNewOnSale.Value;
                await this.SubscriptionService.ChangeSubscription(
                        changeType: model.Subscriptions.DailyNewOnSale.Value ? ChangeSubscriptionType.Create : ChangeSubscriptionType.Delete,
                        token: chatRoom.Token!,
                        subscriptionType: SubscriptionType.DailyNewOnSale,
                        code: null
                    );
            }

            if (model.Subscriptions.DailyNewBestBuy is not null)
            {
                chatRoom!.Subscriptions.DailyNewBestBuy = model.Subscriptions.DailyNewBestBuy.Value;
                await this.SubscriptionService.ChangeSubscription(
                        changeType: model.Subscriptions.DailyNewBestBuy.Value ? ChangeSubscriptionType.Create : ChangeSubscriptionType.Delete,
                        token: chatRoom.Token!,
                        subscriptionType: SubscriptionType.DailyNewBestBuy,
                        code: null
                    );
            }

            if (model.Subscriptions.InventoryCheckList is not null)
            {
                var checkItems = model.Subscriptions.InventoryCheckList.Keys.ToList();
                await UpdateInventoryCheckItems(chatRoom.Id!, checkItems);
                chatRoom!.Subscriptions.InventoryCheckList = checkItems;
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
            // Send to Subscription microservice task
            var unsubTask = new List<Task>();
            var subTask = new List<Task>();

            var chatRoom = await this.ChatRoomRepo.GetById(chatRoomId);
            if (chatRoom is null)
                throw new Exception("ChatRoom not exists");

            var oid = new ObjectId(chatRoom.Id);
            var current = chatRoom.Subscriptions.InventoryCheckList;

            var unsubscriptionItems = current.Except(items);

            var newSubscriptionItems = items.Except(current);

            foreach (var code in unsubscriptionItems)
            {
                var checkItem = await this.InventoryCheckRepo.GetByItemCode(code);

                if (checkItem is null)
                    continue;

                if (checkItem.SubscriptionChatRoom.Contains(oid))
                {
                    var newSubsChatRooms = checkItem.SubscriptionChatRoom.ToHashSet();
                    newSubsChatRooms.Remove(new ObjectId(chatRoomId));
                    checkItem.SubscriptionChatRoom = newSubsChatRooms.ToArray();

                    if (newSubsChatRooms.Count == 0)
                        await this.InventoryCheckRepo.DeleteCheckItemByCode(checkItem.Code);
                    else
                        await this.InventoryCheckRepo.UpdateCheckItem(checkItem);
                }

                // Request Subscription microservice remove subescription
                _ = unsubTask.Append(this.SubscriptionService.ChangeSubscription(
                    ChangeSubscriptionType.Delete, chatRoom.Token!, SubscriptionType.InventoryCheck, code));
            }


            var client = this.HttpClientFactory.CreateClient("default");
            foreach (var code in newSubscriptionItems)
            {
                var checkItem = await this.InventoryCheckRepo.GetByItemCode(code);
                if (checkItem is null)
                {
                    var result = await client.GetFromJsonAsync<CostcoProductInformation>($"https://www.costco.com.tw/rest/v2/taiwan/metadata/productDetails?code={code}");


                    if (result is null || string.IsNullOrWhiteSpace(result.MetaTitle))
                        continue;

                    var newItem = new InventoryCheckItem
                    {
                        Code = code,
                        Name = result.MetaTitle,
                        SubscriptionChatRoom = new[] { oid }
                    };

                    await this.InventoryCheckRepo.CreateNewCheckItem(newItem);
                }
                else
                {
                    var newSubsChatRooms = checkItem.SubscriptionChatRoom.ToHashSet();
                    newSubsChatRooms.Add(oid);
                    checkItem.SubscriptionChatRoom = newSubsChatRooms.ToArray();
                    await this.InventoryCheckRepo.UpdateCheckItem(checkItem);
                }

                // Request Subscription microservice append new subescription
                _ = unsubTask.Append(this.SubscriptionService.ChangeSubscription(
                    ChangeSubscriptionType.Create, chatRoom.Token!, SubscriptionType.InventoryCheck, code));
            }

            await Task.WhenAll(unsubTask);
            await Task.WhenAll(subTask);
        }

        private void EnsureChatRoomExists(LineChatRoom? chatRoom)
        {
            if (chatRoom is null || chatRoom.OwnerId != this.UserId)
                throw new Exception("the specific room id is not exists");
        }
    }
}
