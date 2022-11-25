namespace LineChatRoomService.Models.Events.Intergration
{
    public class RegisterSubscription : IntergrationEvent
    {

        public string SubscriberType { get; set; }

        public string Subscriber { get; set; }

        public string SubscriptionType { get; set; }

        public string? Code { get; set; }
    }
}
