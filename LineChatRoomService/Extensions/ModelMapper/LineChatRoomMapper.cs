using LineChatRoomService.Models;
using LineChatRoomService.Models.Mongo;

namespace LineChatRoomService.Extensions.ModelMapper
{
    public static class LineChatRoomMapper
    {


        public static ChatRoomViewModel ToChatRoomViewModel(this LineChatRoom lineChatRoom)
            => new ChatRoomViewModel
            {
                Id = lineChatRoom.Id,
                RoomName = lineChatRoom.RoomName,
                RoomType = lineChatRoom.RoomType,
                CreateAt = lineChatRoom.CreateAt,
                Subscriptions = lineChatRoom.Subscriptions.ToSubscriptionsViewModel(),
            };

        public static SubscriptionsViewModel ToSubscriptionsViewModel(this Subscriptions subscriptions)
            => new SubscriptionsViewModel
            {
                DailyNewBestBuy = subscriptions.DailyNewBestBuy,
                DailyNewOnSale = subscriptions.DailyNewOnSale,
                InventoryCheckList = subscriptions.InventoryCheckList,
            };

    }
}
