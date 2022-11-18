using LineChatRoomService.Models;
using LineChatRoomService.Services;
using System.Net.Http.Json;
using System.Net;
using Microsoft.Extensions.Logging;
using LineChatRoomService.Models.Microservice;

namespace LineChatRoomServiceTest.Services
{
    public class SubscriptionServiceTest
    {
        [Fact]

        public async void Test_ChangeSubscription_will_call_microservice_endpoint()
        {

            var factory = Substitute.For<IHttpClientFactory>();
            var mockClientHandler = new MockHttpMessageHandler();
            factory.CreateClient().ReturnsForAnyArgs(new HttpClient(mockClientHandler));

            var service = new SubscriptionService(Substitute.For<ILogger<SubscriptionService>>(), factory, "https://localhost/updateEndpoint");

            // Action
            await service.ChangeSubscription(ChangeSubscriptionType.Create, "Token", SubscriptionType.DailyNewBestBuy, null);

            // Assert
            Assert.Equal(1, mockClientHandler.RevievedCount);
            Assert.Equal("https://localhost/updateEndpoint", mockClientHandler.Request.RequestUri.AbsoluteUri.ToString());
            var reqContent = await mockClientHandler.Request.Content.ReadFromJsonAsync<ChangeSubscriptionRequest>();
            Assert.Equal(ChangeSubscriptionType.Create, reqContent.requestType);
            Assert.Equal("Token", reqContent.token);
            Assert.Equal(SubscriptionType.DailyNewBestBuy, reqContent.subscriptionType);
            Assert.Equal(HttpMethod.Patch, mockClientHandler.Request.Method);
        }


        [Fact]
        public async void Test_DeleteAllSubscription_will_call_microservice_endpoint()
        {
            var factory = Substitute.For<IHttpClientFactory>();
            var mockClientHandler = new MockHttpMessageHandler();
            factory.CreateClient().ReturnsForAnyArgs(new HttpClient(mockClientHandler));

            var service = new SubscriptionService(Substitute.For<ILogger<SubscriptionService>>(), factory, "https://localhost/updateEndpoint");

            // Action
            await service.DeleteAllSubscription("Token");

            // Assert
            Assert.Equal(1, mockClientHandler.RevievedCount);
            Assert.Equal("https://localhost/updateEndpoint", mockClientHandler.Request.RequestUri.AbsoluteUri.ToString());
            Assert.Equal(HttpMethod.Patch, mockClientHandler.Request.Method);
            var reqContent = await mockClientHandler.Request.Content.ReadFromJsonAsync<ChangeSubscriptionRequest>();
            Assert.Equal(ChangeSubscriptionType.Delete, reqContent.requestType);
            Assert.Null(reqContent.code);
            Assert.Null(reqContent.subscriptionType);
            Assert.Equal("Token", reqContent.token);
        }
    }
}
