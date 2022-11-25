namespace LineChatRoomService.Models.Events.Intergration
{
    public class RemoveSubscriber : IntergrationEvent
    {
        public string SubscriberType { get; set; }

        public string Subscriber { get; set; }
    }
}
