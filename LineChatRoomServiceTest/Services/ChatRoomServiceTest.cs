using LineChatRoomService.Models;
using LineChatRoomService.Models.Microservice;
using LineChatRoomService.Models.Mongo;
using LineChatRoomService.Repositories.Interface;
using LineChatRoomService.Services;
using LineChatRoomService.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using MongoDB.Bson;
using NSubstitute;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;

namespace LineChatRoomServiceTest.Services
{
    public class ChatRoomServiceTest
    {
        private IHttpContextAccessor CreateMockContext(string userName)
        {
            var context = Substitute.For<HttpContext>();
            context.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userName) }));
            var contextAccessor = Substitute.For<IHttpContextAccessor>();
            contextAccessor.HttpContext = context;

            return contextAccessor;
        }

        public (string userName, string chatRoomId, string roomToken) CreateFakeInfo()
        {
            var userName = "MakotoAtsu";
            var roomId = new ObjectId().ToString();
            var roomToken = "RoomToken";

            return (userName, roomId, roomToken);
        }

        public LineChatRoom CreateFakeChatRoom(string id, string token, string owner)
            => new LineChatRoom
            {
                Id = id,
                OwnerId = owner,
                Token = token
            };


        [Fact]
        public async void Test_CreateChatRoom_will_save_data_into_repository()
        {
            var repo = Substitute.For<IChatRoomRepository>();

            var lineNotifyService = Substitute.For<ILineNotifyService>();

            lineNotifyService.GetChatRoomInfomation("").ReturnsForAnyArgs(new ChatRoomInformation
            {
                target = "MakotoAtsu",
                targetType = "user"
            });

            repo.Create(Arg.Any<LineChatRoom>()).ReturnsForAnyArgs(new LineChatRoom
            {
                Id = ""
            });

            var service = new ChatRoomService(Substitute.For<ILogger<ChatRoomService>>(),
                                              Substitute.For<IHttpContextAccessor>(),
                                              Substitute.For<IHttpClientFactory>(),
                                              lineNotifyService,
                                              repo,
                                              Substitute.For<IInventoryCheckRepository>(),
                                              Substitute.For<ISubscriptionService>());


            await service.CreateChatRoom("owneredId", "Token");

            await repo.Received().Create(Arg.Is<LineChatRoom>(x =>
                    x.Token!.Equals("Token") &&
                    x.OwnerId!.Equals("owneredId") &&
                    x.RoomType!.Equals("user") &&
                    x.RoomName!.Equals("MakotoAtsu")
                ));
        }


        [Fact]
        public async void Test_RevokeChatRoom_will_delete_chatRoom_and_revoke_roomToken_and_delete_all_subscription()
        {
            var (userName, chatRoomId, roomToken) = CreateFakeInfo();

            var repo = Substitute.For<IChatRoomRepository>();
            var lineNotifyService = Substitute.For<ILineNotifyService>();
            var contextAccessor = CreateMockContext(userName);
            var subscriptionService = Substitute.For<ISubscriptionService>();


            repo.GetById("").ReturnsForAnyArgs(CreateFakeChatRoom(chatRoomId, roomToken, userName));

            var service = new ChatRoomService(Substitute.For<ILogger<ChatRoomService>>(),
                                              contextAccessor,
                                              Substitute.For<IHttpClientFactory>(),
                                              lineNotifyService,
                                              repo,
                                              Substitute.For<IInventoryCheckRepository>(),
                                              subscriptionService);



            // Action
            await service.RevokeChatRoom(chatRoomId);


            // Assert
            await repo.Received(1).Delete(Arg.Is<LineChatRoom>(x => x.Id == chatRoomId));
            await lineNotifyService.Received(1).RevokeChatRoom(roomToken);
            await subscriptionService.Received(1).DeleteAllSubscription(roomToken);

        }


        [Fact]
        public async void Test_SendMessageToChatRoom_will_call_lineNotifyService()
        {
            var (userName, chatRoomId, roomToken) = CreateFakeInfo();

            var lineNotifyService = Substitute.For<ILineNotifyService>();
            var contextAccessor = CreateMockContext(userName);
            var repo = Substitute.For<IChatRoomRepository>();

            repo.GetById("").ReturnsForAnyArgs(CreateFakeChatRoom(chatRoomId, roomToken, userName));

            var service = new ChatRoomService(Substitute.For<ILogger<ChatRoomService>>(),
                                             contextAccessor,
                                             Substitute.For<IHttpClientFactory>(),
                                             lineNotifyService,
                                             repo,
                                             Substitute.For<IInventoryCheckRepository>(),
                                             Substitute.For<ISubscriptionService>());


            // Action
            await service.SendMessageToChatRoom(chatRoomId, "HAHA");

            // Assert
            await lineNotifyService.Received(1).SendMessage(roomToken, "HAHA");
        }


        [Fact]
        public async void Test_GetChatRoomById_will_fetch_product_name_if_it_has_inventoryCheck_item()
        {
            var (userName, chatRoomId, roomToken) = CreateFakeInfo();

            var lineNotifyService = Substitute.For<ILineNotifyService>();
            var contextAccessor = CreateMockContext(userName);
            var repo = Substitute.For<IChatRoomRepository>();

            var chatRoom = CreateFakeChatRoom(chatRoomId, roomToken, userName);
            chatRoom.Subscriptions.InventoryCheckList.AddRange(new[] { "123456", "654321" });

            repo.GetById("").ReturnsForAnyArgs(chatRoom);

            var inventoryCheckRepo = Substitute.For<IInventoryCheckRepository>();

            inventoryCheckRepo.GetByItemCode("123456").Returns(new InventoryCheckItem
            {
                Code = "123456",
                Name = "Item1"
            });

            inventoryCheckRepo.GetByItemCode("654321").Returns(new InventoryCheckItem
            {
                Code = "654321",
                Name = "Item2"
            });

            var service = new ChatRoomService(Substitute.For<ILogger<ChatRoomService>>(),
                                             contextAccessor,
                                             Substitute.For<IHttpClientFactory>(),
                                             lineNotifyService,
                                             repo,
                                             inventoryCheckRepo,
                                             Substitute.For<ISubscriptionService>());


            // Action
            var result = await service.GetChatRoomById(chatRoomId);


            await inventoryCheckRepo.ReceivedWithAnyArgs(2).GetByItemCode(Arg.Any<string>());
            Assert.Contains(result!.Subscriptions!.InventoryCheckList, x => x.Key == "123456" && x.Value == "Item1");
            Assert.Contains(result!.Subscriptions!.InventoryCheckList, x => x.Key == "654321" && x.Value == "Item2");

        }


        [Fact]
        public async void Test_GetAllChatRooms_will_fetch_product_name_if_any_have_inventoryCheck_item()
        {

            var lineNotifyService = Substitute.For<ILineNotifyService>();
            var contextAccessor = CreateMockContext("MakotoAtsu");
            var repo = Substitute.For<IChatRoomRepository>();

            var userName = "MakotoAtsu";
            var roomId1 = "11111111111111111111111111111";
            var roomId2 = "22222222222222222222222222222";

            var chatRoom1 = CreateFakeChatRoom(roomId1, "token11", userName);
            var chatRoom2 = CreateFakeChatRoom(roomId2, "token22", userName);

            chatRoom1.Subscriptions.InventoryCheckList.AddRange(new[] { "123456", "654321" });
            chatRoom2.Subscriptions.InventoryCheckList.Add("123321");


            repo.GetByOwner("").ReturnsForAnyArgs(new List<LineChatRoom> { chatRoom1, chatRoom2 });

            var inventoryCheckRepo = Substitute.For<IInventoryCheckRepository>();

            inventoryCheckRepo.GetByItemCode("123456").Returns(new InventoryCheckItem
            {
                Code = "123456",
                Name = "Item1"
            });

            inventoryCheckRepo.GetByItemCode("654321").Returns(new InventoryCheckItem
            {
                Code = "654321",
                Name = "Item2"
            });


            var service = new ChatRoomService(Substitute.For<ILogger<ChatRoomService>>(),
                                             contextAccessor,
                                             Substitute.For<IHttpClientFactory>(),
                                             lineNotifyService,
                                             repo,
                                             inventoryCheckRepo,
                                             Substitute.For<ISubscriptionService>());


            // Action
            var result = await service.GetAllChatRooms();


            // Assert
            await inventoryCheckRepo.ReceivedWithAnyArgs(3).GetByItemCode(Arg.Any<string>());
            Assert.Equal(2, result.Count());

        }

        [Fact]
        public async void Test_UpdateChatRoom_will_send_LineNotofy_message_to_user()
        {
            var (userName, chatRoomId, roomToken) = CreateFakeInfo();

            var lineNotifyService = Substitute.For<ILineNotifyService>();
            var contextAccessor = CreateMockContext(userName);

            var chatRoom = CreateFakeChatRoom(chatRoomId, roomToken, userName);
            var repo = Substitute.For<IChatRoomRepository>();

            repo.GetById("").ReturnsForAnyArgs(chatRoom);

            var updateModel = new ChatRoomViewModel
            {
                Id = chatRoomId,
                Subscriptions = new SubscriptionsViewModel()
            };


            var service = new ChatRoomService(Substitute.For<ILogger<ChatRoomService>>(),
                                             contextAccessor,
                                             Substitute.For<IHttpClientFactory>(),
                                             lineNotifyService,
                                             repo,
                                             Substitute.For<IInventoryCheckRepository>(),
                                             Substitute.For<ISubscriptionService>());


            // Action
            await service.UpdateChatRoom(updateModel);


            // Assert
            await lineNotifyService.Received(1).SendMessage(roomToken, Arg.Any<string>());
        }


        [Fact]
        public async void Test_UpdateChatRoom_will_call_SubscriptionService()
        {
            var (userName, chatRoomId, roomToken) = CreateFakeInfo();

            var contextAccessor = CreateMockContext(userName);

            var chatRoom = CreateFakeChatRoom(chatRoomId, roomToken, userName);
            var repo = Substitute.For<IChatRoomRepository>();

            repo.GetById("").ReturnsForAnyArgs(chatRoom);

            var updateModel = new ChatRoomViewModel
            {
                Id = chatRoomId,
                Subscriptions = new SubscriptionsViewModel
                {
                    DailyNewBestBuy = true,
                    DailyNewOnSale = true,
                }
            };

            var subscriptionService = Substitute.For<ISubscriptionService>();

            var service = new ChatRoomService(Substitute.For<ILogger<ChatRoomService>>(),
                                             contextAccessor,
                                             Substitute.For<IHttpClientFactory>(),
                                             Substitute.For<ILineNotifyService>(),
                                             repo,
                                             Substitute.For<IInventoryCheckRepository>(),
                                             subscriptionService);

            // Action
            await service.UpdateChatRoom(updateModel);


            // Assert 
            await subscriptionService.Received(2).ChangeSubscription(Arg.Any<ChangeSubscriptionType>(), roomToken, Arg.Any<SubscriptionType>(), null);
        }

        [Fact]
        public async void Test_UpdateChatRoom_will_fetch_product_info_if_create_new_inventoryCheck_item()
        {
            var (userName, chatRoomId, roomToken) = CreateFakeInfo();

            var contextAccessor = CreateMockContext(userName);

            var repo = Substitute.For<IChatRoomRepository>();

            repo.GetById("").ReturnsForAnyArgs(CreateFakeChatRoom(chatRoomId, roomToken, userName));


            var subscriptionService = Substitute.For<ISubscriptionService>();

            var factory = Substitute.For<IHttpClientFactory>();
            var mockClientHandler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new CostcoProductInformation
                {
                    MetaTitle = "Item Name"
                })
            });
            factory.CreateClient().ReturnsForAnyArgs(new HttpClient(mockClientHandler));

            var service = new ChatRoomService(Substitute.For<ILogger<ChatRoomService>>(),
                                             contextAccessor,
                                             factory,
                                             Substitute.For<ILineNotifyService>(),
                                             repo,
                                             Substitute.For<IInventoryCheckRepository>(),
                                             subscriptionService);


            var updateModel = new ChatRoomViewModel
            {
                Id = chatRoomId,
                Subscriptions = new SubscriptionsViewModel
                {
                    InventoryCheckList = new Dictionary<string, string>
                    {
                        { "123456", "" },
                    }
                }
            };


            // Action
            await service.UpdateChatRoom(updateModel);


            // Assert
            await subscriptionService.Received(1).ChangeSubscription(ChangeSubscriptionType.Create, roomToken, SubscriptionType.InventoryCheck, "123456");
            Assert.Equal(1, mockClientHandler.RevievedCount);
            Assert.Equal("https://www.costco.com.tw/rest/v2/taiwan/metadata/productDetails?code=123456", mockClientHandler.Request.RequestUri.ToString());

        }
    }

}
