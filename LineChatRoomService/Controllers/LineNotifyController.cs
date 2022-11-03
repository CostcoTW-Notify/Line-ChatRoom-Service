using LineChatRoomService.Extensions;
using LineChatRoomService.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LineChatRoomService.Controllers
{

    public class LineNotifyController : Controller
    {

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

            var url = service.BuildChallengeUrl(redirect_uri, user);
            return Ok(url);
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

            var (redirectUrl, user) = lineService.GetInfoFromState(state);

            var token = await lineService.ExchangeCodeAsync(code);

            if (string.IsNullOrEmpty(token))
                return BadRequest();

            try
            {
                await chatRoomService.CreateChatRoom(user, token);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                await lineService.RevokeChatRoom(token);
            }

            return Redirect(redirectUrl);
        }
    }
}
