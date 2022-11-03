using LineChatRoomService.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LineChatRoomService.Controllers
{

    public class LineNotifyController : Controller
    {

        [AllowAnonymous]
        [HttpGet("/LineNotify/RegisterNewChatRoom")]
        public IActionResult CreateNewChatRoom(
            [FromQuery] string redirect_uri,
            [FromServices] ILineNotifyService service)
        {
            if (string.IsNullOrWhiteSpace(redirect_uri))
                return BadRequest(new
                {
                    message = "redirect_uri is required"
                });
            var user = "testUser";
            var url = service.BuildChallengeUrl(redirect_uri, user);
            return Redirect(url);
        }

        [AllowAnonymous]
        [HttpGet("/LineNotify/callback")]
        public async Task<IActionResult> Callback(
            [FromQuery] string code,
            [FromQuery] string state,
            [FromServices] ILineNotifyService service)
        {
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
                return BadRequest();

            var (redirectUrl, user) = service.GetInfoFromState(state);

            var token = await service.ExchangeCodeAsync(code);

            if (string.IsNullOrEmpty(token))
                return BadRequest();

            var roomInfo = await service.GetChatRoomInfomation(token);

            if (roomInfo is null)
                return BadRequest();

            // todo: Store room date


            return Redirect(redirectUrl);
        }
    }
}
