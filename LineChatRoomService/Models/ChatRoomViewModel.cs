namespace LineChatRoomService.Models
{
    public class ChatRoomViewModel
    {
        public string? Id { get; set; }

        public string? RoomName { get; set; }

        public string? RoomType { get; set; }

        public DateTime CreateAt { get; set; }

        public SubscriptionsViewModel? Subscriptions { get; set; }
    }
}
