using LineChatRoomService.Models;
using LineChatRoomService.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LineChatRoomService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatRoomsController : ControllerBase
    {
        private readonly ILogger<ChatRoomsController> _log;

        public IChatRoomService ChatRoomService { get; }

        public ChatRoomsController(
            ILogger<ChatRoomsController> logger,
            IChatRoomService service)
        {
            this._log = logger;
            this.ChatRoomService = service;
        }

        [HttpGet]
        public async Task<List<ChatRoomViewModel>> GetAllChatRoom()
        {
            var results = await ChatRoomService.GetAllChatRooms();
            return results.ToList();
        }

        [HttpGet("{chatRoomId}")]
        public async Task<ChatRoomViewModel?> GetChatRoomById(string chatRoomId)
        {
            var result = await ChatRoomService.GetChatRoomById(chatRoomId);
            return result;
        }


        [HttpPatch("{chatRoomId}")]
        public async Task<IActionResult> UpdateChatRoomInfo([FromRoute] string chatRoomId, [FromBody] ChatRoomViewModel model)
        {
            model.Id = chatRoomId;

            if (model.Subscriptions is null)
                return BadRequest("Subscriptions is required");

            await ChatRoomService.UpdateChatRoom(model);

            return Ok();
        }

        [HttpDelete("{chatRoomId}")]
        public async Task RemoveChatRoomById(string chatRoomId)
        {
            await this.ChatRoomService.RevokeChatRoom(chatRoomId);
        }


    }

}