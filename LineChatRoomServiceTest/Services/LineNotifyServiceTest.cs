using LineChatRoomService.Models;
using LineChatRoomService.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Policy;

namespace LineChatRoomServiceTest.Services
{
    public class LineNotifyServiceTest
    {
        [Fact]
        public async void Test_RevokeChatRoom_will_send_correct_request()
        {
            var factory = Substitute.For<IHttpClientFactory>();
            var mockClientHandler = new MockHttpMessageHandler();
            factory.CreateClient().ReturnsForAnyArgs(new HttpClient(mockClientHandler));


            var service = new LineNotifyService(Substitute.For<ILogger<LineNotifyService>>(),
                                                "clientId",
                                                "clientSecret",
                                                factory,
                                                Substitute.For<IHttpContextAccessor>(),
                                                Substitute.For<IDataProtectionProvider>()
                                                );


            await service.RevokeChatRoom("TokenTokenTokenTokenToken");


            Assert.Equal(1, mockClientHandler.RevievedCount);
            Assert.Equal("https://notify-api.line.me/api/revoke", mockClientHandler.Request.RequestUri?.AbsoluteUri);
            Assert.Equal("TokenTokenTokenTokenToken", mockClientHandler.Request.Headers.Authorization?.Parameter);
            Assert.Equal("Bearer", mockClientHandler.Request.Headers.Authorization?.Scheme);
        }


        [Fact]
        public async void Test_GetChatRoomInfomation_will_send_correct_request()
        {
            var factory = Substitute.For<IHttpClientFactory>();
            var mockClientHandler = new MockHttpMessageHandler();
            factory.CreateClient().ReturnsForAnyArgs(new HttpClient(mockClientHandler));
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new ChatRoomInformation
                {
                    target = "MakotoAtsu",
                    targetType = "user"
                })
            };
            mockClientHandler.Response = fakeResponse;

            var service = new LineNotifyService(Substitute.For<ILogger<LineNotifyService>>(),
                                                "clientId",
                                                "clientSecret",
                                                factory,
                                                Substitute.For<IHttpContextAccessor>(),
                                                Substitute.For<IDataProtectionProvider>()
                                                );


            var info = await service.GetChatRoomInfomation("TokenTokenTokenTokenToken");


            Assert.Equal(1, mockClientHandler.RevievedCount);
            Assert.Equal("https://notify-api.line.me/api/status", mockClientHandler.Request.RequestUri?.AbsoluteUri);
            Assert.Equal("TokenTokenTokenTokenToken", mockClientHandler.Request.Headers.Authorization?.Parameter);
            Assert.Equal("Bearer", mockClientHandler.Request.Headers.Authorization?.Scheme);
            Assert.Equal("MakotoAtsu", info.target);
            Assert.Equal("user", info.targetType);
        }


        [Fact]
        public async void Test_ExchangeCodeAsync_Send_correct_request()
        {
            var factory = Substitute.For<IHttpClientFactory>();
            var mockClientHandler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    access_token = "Access_token"
                })
            });
            factory.CreateClient().ReturnsForAnyArgs(new HttpClient(mockClientHandler));


            var service = new LineNotifyService(Substitute.For<ILogger<LineNotifyService>>(),
                                                "clientId",
                                                "clientSecret",
                                                factory,
                                                Substitute.For<IHttpContextAccessor>(),
                                                Substitute.For<IDataProtectionProvider>()
                                                );


            var result = await service.ExchangeCodeAsync("issue_code");


            Assert.Equal(1, mockClientHandler.RevievedCount);
            Assert.Equal("https://notify-bot.line.me/oauth/token", mockClientHandler.Request.RequestUri?.AbsoluteUri);
            Assert.Equal("clientId", mockClientHandler.FormData["client_id"]);
            Assert.Equal("clientSecret", mockClientHandler.FormData["client_secret"]);
            Assert.Equal("authorization_code", mockClientHandler.FormData["grant_type"]);
            Assert.Equal("issue_code", mockClientHandler.FormData["code"]);
            Assert.NotNull(mockClientHandler.FormData["redirect_uri"]);
            Assert.Equal("Access_token", result);

        }


        [Fact]
        public async void Test_BuildChallengeUrl_will_return_correct_url()
        {
            var provider = Substitute.For<IDataProtectionProvider>();
            var protector = Substitute.For<IDataProtector>();
            provider.CreateProtector(string.Empty).ReturnsForAnyArgs(protector);

            var service = new LineNotifyService(Substitute.For<ILogger<LineNotifyService>>(),
                                               "clientId",
                                               "clientSecret",
                                               Substitute.For<IHttpClientFactory>(),
                                               Substitute.For<IHttpContextAccessor>(),
                                               provider
                                               );

            var result = service.BuildChallengeUrl("redirect_url", "MakotoAtsu");

            Assert.NotNull(result);
            var uri = new Uri(result);
            var querys = uri.Query.Substring(1).Split('&');
            Assert.Contains("client_id=clientId", querys);
            Assert.Contains("scope=notify", querys);
            Assert.Contains("response_type=code", querys);
            Assert.Contains(querys, q => q.StartsWith("redirect_uri="));
            Assert.Contains(querys, q => q.StartsWith("state="));
        }
    }


    public class MockHttpMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage Request { get; private set; }

        public IDictionary<string, string> FormData { get; private set; }

        public int RevievedCount { get; private set; } = 0;

        public HttpResponseMessage Response { get; set; }

        public MockHttpMessageHandler(HttpResponseMessage response = null)
        {
            this.Response = response;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            this.RevievedCount += 1;
            this.Request = request;

            if (request.Content.Headers.ContentType?.MediaType == "application/x-www-form-urlencoded")
            {
                this.FormData = new Dictionary<string, string>();
                var data = await request.Content.ReadAsStringAsync();
                foreach (var item in data.Split('&'))
                {
                    var temp = item.Split('=');
                    this.FormData[temp[0]] = temp[1];
                }

            }


            if (Response is null)
                return new HttpResponseMessage(HttpStatusCode.OK);
            return this.Response;
        }
    }
}
