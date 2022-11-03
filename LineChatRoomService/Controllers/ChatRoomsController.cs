using Microsoft.AspNetCore.Mvc;

namespace LineChatRoomService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatRoomsController : ControllerBase
    {

        [HttpGet]
        public IActionResult GetAllChatRoom()
        {
            // Get All Chat Room By UserId
            throw new NotImplementedException();
        }

        [HttpGet("{chatRoomId}")]
        public IActionResult GetChatRoomById(string chatRoomId)
        {
            // Get ChatRoom By Room Id note: need to check room owner
            throw new NotImplementedException();
        }


        [HttpPatch("{chatRoomId}")]
        public IActionResult UpdateChatRoomInfo(string chatRoomId)
        {
            // Update Room notify type
            throw new NotImplementedException();
        }

        [HttpDelete("{chatRoomId}")]
        public string RemoveChatRoomById(string chatRoomId)
        {
            // Remove Room and revoke token
            throw new NotImplementedException();
        }


    }

}