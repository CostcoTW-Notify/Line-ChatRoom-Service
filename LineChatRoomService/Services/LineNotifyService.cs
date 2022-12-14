using LineChatRoomService.Models;
using LineChatRoomService.Services.Interface;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using System.Net.Http.Headers;
using System.Text.Json;

namespace LineChatRoomService.Services
{

    public class LineNotifyService : ILineNotifyService
    {
        private readonly ILogger<LineNotifyService> _log;

        public string ClientId { get; }

        public string ClientSecret { get; }

        public IHttpClientFactory HttpClientFactory { get; }

        public HttpContext? HttpContext { get; }

        public IDataProtectionProvider DataProtectionProvider { get; }

        public const string CallbackEndpoint = "/LineNotify/callback";
        public const string TokenEndpoint = "https://notify-bot.line.me/oauth/token";
        public const string AuthorizationEndpoint = "https://notify-bot.line.me/oauth/authorize";
        public const string ChatRoomStateEndpoint = "https://notify-api.line.me/api/status";
        public const string SendMessageEndpoint = "https://notify-api.line.me/api/notify";
        public const string RevokeRoomTokenEndpoint = "https://notify-api.line.me/api/revoke";


        public LineNotifyService(
            ILogger<LineNotifyService> logger,
            string lineClientId,
            string lineClientSecret,
            IHttpClientFactory clientFactory,
            IHttpContextAccessor httpContextAccessor,
            IDataProtectionProvider provider)
        {
            this._log = logger;
            ClientId = lineClientId;
            ClientSecret = lineClientSecret;
            HttpClientFactory = clientFactory;
            HttpContext = httpContextAccessor.HttpContext;
            this.DataProtectionProvider = provider;
        }

        protected class State
        {
            public string? RedirectUrl { get; set; }
            public string? User { get; set; }
            public DateTime? CreateAt { get; set; } = DateTime.Now;
        }

        public string GenerateState(string redirectUri, string user)
        {
            var state = new State
            {
                RedirectUrl = redirectUri,
                User = user
            };

            var jsonState = JsonSerializer.Serialize(state);
            var protector = this.DataProtectionProvider.CreateProtector("line-state");
            var encryptState = protector.Protect(jsonState);
            return encryptState;
        }

        public (string redirectUrl, string user, DateTime createAt) GetInfoFromState(string state)
        {
            var protector = this.DataProtectionProvider.CreateProtector("line-state");
            var json = protector.Unprotect(state);
            var state_obj = JsonSerializer.Deserialize<State>(json);
            if (state_obj is null ||
                string.IsNullOrWhiteSpace(state_obj.RedirectUrl) ||
                string.IsNullOrWhiteSpace(state_obj.User) ||
                state_obj.CreateAt is null)
                throw new Exception("wrong state..");

            return (state_obj.RedirectUrl, state_obj.User, state_obj.CreateAt.Value);
        }


        public string BuildChallengeUrl(string redirectUri, string user)
        {
            var current_calback_path = $"{HttpContext!.Request.Scheme}://{HttpContext!.Request.Host}{CallbackEndpoint}";
            var parameters = new Dictionary<string, string>
            {
                { "client_id", ClientId },
                { "scope", "notify" },
                { "response_type", "code" },
                { "redirect_uri", current_calback_path },
                { "state", GenerateState(redirectUri,user)}
            };

            return QueryHelpers.AddQueryString(AuthorizationEndpoint, parameters!);
        }

        public async Task<string?> ExchangeCodeAsync(string code)
        {
            var current_calback_path = $"{HttpContext!.Request.Scheme}://{HttpContext!.Request.Host}{CallbackEndpoint}";

            var tokenRequestParameters = new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = current_calback_path,
                ["client_id"] = ClientId,
                ["client_secret"] = ClientSecret,
            };


            using var request = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint);
            request.Content = new FormUrlEncodedContent(tokenRequestParameters);

            var client = CreateHttpClient();
            using var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var error = response.Content.ReadAsStringAsync();
                _log.LogError("Exchange token fail... response : " + error);
            }

            var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (payload.RootElement.TryGetProperty("access_token", out var tokenProperty))
                return tokenProperty.GetString()!;
            else
                return null;
        }


        public async Task<ChatRoomInformation?> GetChatRoomInfomation(string roomToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, ChatRoomStateEndpoint);

            var httpClient = CreateHttpClient(roomToken);
            using var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            var payload = JsonSerializer.Deserialize<ChatRoomInformation>(json);
            return payload;
        }

        public async Task<bool> SendMessage(string token, string testMessage)
        {
            var message = new Dictionary<string, string>
            {
                ["message"] = "\n" + testMessage,
            };

            var client = CreateHttpClient(token);

            using var request = new HttpRequestMessage(HttpMethod.Post, SendMessageEndpoint);
            request.Content = new FormUrlEncodedContent(message);

            using var response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                return true;
            }
            else
            {
                var result = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Send Test message fail ..." + result);
                return false;
            }
        }

        public async Task RevokeChatRoom(string token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, RevokeRoomTokenEndpoint);

            var client = CreateHttpClient(token);
            using var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                _log.LogError($"Revoke chat room token fail... respoinse : {result}");
            }
            else
            {
                _log.LogInformation($"Revoke chat room token : {token}");
            }
        }


        private HttpClient CreateHttpClient(string? roomToken = null)
        {
            var httpClient = HttpClientFactory.CreateClient("default");
            if (!string.IsNullOrWhiteSpace(roomToken))
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", roomToken);

            return httpClient;
        }
    }
}
