using LineChatRoomService.Models.Microservice;
using LineChatRoomService.Services.Interface;
using LineChatRoomService.Utility;
using System.Diagnostics.CodeAnalysis;

namespace LineChatRoomService.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly ILogger<SubscriptionService> _log;

        public IHttpClientFactory HttpClientFactory { get; }

        public string SubscriptionEndpoint { get; }


        public SubscriptionService(ILogger<SubscriptionService> logger, IHttpClientFactory factory, string subscriptionEndpoint)
        {
            this._log = logger;
            this.HttpClientFactory = factory;
            this.SubscriptionEndpoint = subscriptionEndpoint;
        }


        public async Task<bool> ChangeSubscription(ChangeSubscriptionType changeType, string token, SubscriptionType? subscriptionType, string? code)
        {
            if (subscriptionType == SubscriptionType.InventoryCheck && string.IsNullOrWhiteSpace(code))
                throw new Exception("Missing 'code'");
            var req = new HttpRequestMessage(HttpMethod.Patch, this.SubscriptionEndpoint);

            var body = new ChangeSubscriptionRequest
            {
                requestType = changeType,
                token = token,
                subscriptionType = subscriptionType,
                code = code,
            };

            req.Content = JsonContent.Create(body);

            var client = this.HttpClientFactory.CreateClient("default");

            var response = await client.SendAsync(req);

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteAllSubscription([NotNull] string token)
        {
            var req = new HttpRequestMessage(HttpMethod.Patch, this.SubscriptionEndpoint);

            var body = new ChangeSubscriptionRequest
            {
                requestType = ChangeSubscriptionType.Delete,
                token = token,
            };

            req.Content = JsonContent.Create(body);

            var client = this.HttpClientFactory.CreateClient("default");

            var response = await client.SendAsync(req);

            return response.IsSuccessStatusCode;
        }
    }
}
