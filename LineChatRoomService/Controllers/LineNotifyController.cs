using LineChatRoomService.Extensions;
using LineChatRoomService.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LineChatRoomService.Controllers
{

    public class LineNotifyController : Controller
    {
        private readonly ILogger<LineNotifyController> _log;

        public LineNotifyController(ILogger<LineNotifyController> logger)
        {
            this._log = logger;
        }

        [HttpGet("/LineNotify/RegisterChatRoomUrl")]
        public IActionResult CreateNewChatRoom(
            [FromQuery] string redirect_uri,
            [FromServices] ILineNotifyService service)
        {
            if (string.IsNullOrWhiteSpace(redirect_uri))
                return BadRequest(new
                {
                    message = "redirect_uri is required"
                });
            var user = HttpContext.GetUserId();

            var url = service.BuildChallengeUrl(redirect_uri, user!);

            return Ok(new
            {
                Register_Url = url
            });
        }


        [AllowAnonymous] // For line callback, bypass authorize
        [HttpGet("/LineNotify/callback")]
        public async Task<IActionResult> Callback(
            [FromQuery] string code,
            [FromQuery] string state,
            [FromServices] ILineNotifyService lineService,
            [FromServices] IChatRoomService chatRoomService)
        {
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
                return BadRequest();


            var (redirectUrl, user, _) = lineService.GetInfoFromState(state);

            var token = await lineService.ExchangeCodeAsync(code);

            if (!string.IsNullOrEmpty(token))
                try
                {
                    await chatRoomService.CreateChatRoom(user, token);
                }
                catch (Exception ex)
                {
                    _log.LogError("Create room caught.. " + ex.ToString());
                    await lineService.RevokeChatRoom(token);

                }

            return Redirect(redirectUrl);
        }
    }
}
