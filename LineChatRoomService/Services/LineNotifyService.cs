using LineChatRoomService.Models;
using LineChatRoomService.Services.Interface;
using LineChatRoomService.Utility;
using Microsoft.AspNetCore.WebUtilities;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;

namespace LineChatRoomService.Services
{

    public class LineNotifyService : ILineNotifyService
    {
        public string ClientId { get; }

        public string ClientSecret { get; }


        public Aes Aes { get; }
        public IHttpClientFactory HttpClientFactory { get; }
        public HttpContext? HttpContext { get; }

        public const string CallbackEndpoint = "/LineNotify/callback";
        public const string TokenEndpoint = "https://notify-bot.line.me/oauth/token";
        public const string AuthorizationEndpoint = "https://notify-bot.line.me/oauth/authorize";
        public const string ChatRoomStateEndpoint = "https://notify-api.line.me/api/status";
        public const string SendMessageEndpoint = "https://notify-api.line.me/api/notify";
        public const string RevokeRoomTokenEndpoint = "https://notify-api.line.me/api/revoke";


        public LineNotifyService(IHttpClientFactory clientFactory, IHttpContextAccessor httpContextAccessor)
        {
            ClientId = Environment.GetEnvironmentVariable("line_client_id")!;
            ClientSecret = Environment.GetEnvironmentVariable("line_client_secret")!;
            Aes = GetAes();
            HttpClientFactory = clientFactory;
            HttpContext = httpContextAccessor.HttpContext;
        }

        private Aes GetAes()
        {
            var aes = Aes.Create();
            var aesKey = Convert.FromBase64String(Environment.GetEnvironmentVariable("AES-Key")!);
            var aesIv = Convert.FromBase64String(Environment.GetEnvironmentVariable("AES-IV")!);
            aes.Key = aesKey;
            aes.IV = aesIv;
            return aes;
        }

        protected class State
        {
            public string? RedirectUrl { get; set; }
            public string? User { get; set; }
        }

        public string GenerateState(string redirectUri, string user)
        {
            var state = new State
            {
                RedirectUrl = redirectUri,
                User = user
            };

            var jsonState = JsonSerializer.Serialize(state);
            var encryptState = AesHelper.EncryptString(Aes, jsonState);
            return encryptState;
        }

        public (string redirectUrl, string user) GetInfoFromState(string state)
        {
            var decrypt = AesHelper.DecryptString(Aes, state);
            var state_obj = JsonSerializer.Deserialize<State>(decrypt);
            if (state_obj is null ||
                string.IsNullOrWhiteSpace(state_obj.RedirectUrl) ||
                string.IsNullOrWhiteSpace(state_obj.User))
                throw new Exception("Cannot get infomation from state..");
            return (state_obj.RedirectUrl, state_obj.User);
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
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new FormUrlEncodedContent(tokenRequestParameters);

            var client = CreateHttpClient();
            using var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var error = response.Content.ReadAsStringAsync();
                Console.WriteLine(error);
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
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var httpClient = CreateHttpClient(roomToken);
            using var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            var payload = JsonSerializer.Deserialize<ChatRoomInformation>(json);
            return payload;
        }

        public async Task<bool> SendMessageToChatRoom(string roomId, string testMessage)
        {
            var message = new Dictionary<string, string>
            {
                ["message"] = testMessage,
            };


            using var request = new HttpRequestMessage(HttpMethod.Post, SendMessageEndpoint);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new FormUrlEncodedContent(message);

            // TODO: Get room token from id 
            var token = "";

            var client = CreateHttpClient(token);
            using var response = await client.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        public async Task RevokeChatRoom(string roomId)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, RevokeRoomTokenEndpoint);

            // TODO: Remove room and get token
            var token = "";

            var client = CreateHttpClient(token);
            using var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                Console.WriteLine(result);
            }

        }


        private HttpClient CreateHttpClient(string? roomToken = null)
        {
            var httpClient = HttpClientFactory.CreateClient();
            if (!string.IsNullOrWhiteSpace(roomToken))
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", roomToken);

            return httpClient;
        }
    }
}
