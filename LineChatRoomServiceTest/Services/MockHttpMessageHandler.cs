using System.Net;

namespace LineChatRoomServiceTest.Services
{
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

            if (request.Content?.Headers.ContentType?.MediaType == "application/x-www-form-urlencoded")
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
