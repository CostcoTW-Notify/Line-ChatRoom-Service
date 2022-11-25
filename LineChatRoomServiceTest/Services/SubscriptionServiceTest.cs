using LineChatRoomService.Models;
using LineChatRoomService.Services;
using System.Net.Http.Json;
using System.Net;
using Microsoft.Extensions.Logging;
using LineChatRoomService.Models.Microservice;
using LineChatRoomService.Services.Interface;
using LineChatRoomService.Models.Events.Intergration;

namespace LineChatRoomServiceTest.Services
{
    public class SubscriptionServiceTest
    {
        [Fact]

        public async void Test_ChangeSubscription_will_call_microservice_endpoint()
        {

            var intergraionService = Substitute.For<IIntergrationEventService>();
            var service = new SubscriptionService(Substitute.For<ILogger<SubscriptionService>>(), intergraionService);


            // Action
            await service.ChangeSubscription(ChangeSubscriptionType.Create, "Token", SubscriptionType.DailyNewBestBuy, null);

            // Assert
            await intergraionService.Received(1)
                .PublishEvent(Arg.Is<RegisterSubscription>(x => x.SubscriberType == "LineNotify" &&
                                                                x.Subscriber == "Token" &&
                                                                x.SubscriptionType == "DailyNewBestBuy" &&
                                                                x.Code == null));
        }


        [Fact]
        public async void Test_DeleteAllSubscription_will_call_microservice_endpoint()
        {
            var intergraionService = Substitute.For<IIntergrationEventService>();
            var service = new SubscriptionService(Substitute.For<ILogger<SubscriptionService>>(), intergraionService);

            // Action
            await service.DeleteAllSubscription("Token");

            // Assert
            await intergraionService.Received(1)
                .PublishEvent(Arg.Is<RemoveSubscriber>(x => x.SubscriberType == "LineNotify" && x.Subscriber == "Token"));
        }
    }
}
