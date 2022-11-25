using LineChatRoomService.Models.Events.Intergration;

namespace LineChatRoomService.Services.Interface
{
    public interface IIntergrationEventService
    {

        Task PublishEvent(IntergrationEvent @event);

    }
}
