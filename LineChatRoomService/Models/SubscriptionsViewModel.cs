namespace LineChatRoomService.Models
{
    public class SubscriptionsViewModel
    {
        /// <summary>
        /// 每日新上架特價商品
        /// </summary>
        public bool? DailyNewOnSale { get; set; }

        /// <summary>
        /// 每日新上架最低價商品 (價格尾數為 7 的商品)
        /// </summary>
        public bool? DailyNewBestBuy { get; set; }

        /// <summary>
        /// 庫存通知商品 Code
        /// </summary>
        public Dictionary<string, string>? InventoryCheckList { set; get; }
    }
}
