using LineChatRoomService.Models.Costco;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LineChatRoomService.Controllers
{
    [ApiController]
    public class CostcoProductController : ControllerBase
    {
        private ILogger<CostcoProductController> _log;

        public IHttpClientFactory HttpClientFactory { get; }

        public CostcoProductController(ILogger<CostcoProductController> logger, IHttpClientFactory factory)
        {
            this._log = logger;
            this.HttpClientFactory = factory;
        }

        [AllowAnonymous]
        [HttpGet("/api/CostcoProduct/Search")]
        public async Task<ActionResult<CostcoProduct>> FetchProductFromCode([FromQuery] string code)
        {

            var client = this.HttpClientFactory.CreateClient("default");
            var result = await client.GetFromJsonAsync<CostcoProduct>($"https://www.costco.com.tw/rest/v2/taiwan/metadata/productDetails?code={code}");

            return Ok(result);
        }

    }
}
