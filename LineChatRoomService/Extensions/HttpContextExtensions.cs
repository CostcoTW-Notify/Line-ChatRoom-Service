using System.Security.Claims;

namespace LineChatRoomService.Extensions
{
    public static class HttpContextExtensions
    {

        public static string GetUserId(this HttpContext context)
            => context.User.Claims.Where(x => x.Type == ClaimTypes.NameIdentifier).FirstOrDefault()!.Value;
    }
}
