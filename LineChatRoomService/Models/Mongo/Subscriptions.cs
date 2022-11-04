namespace LineChatRoomService.Models.Mongo
{
    public class Subscriptions
    {
        /// <summary>
        /// 每日新上架特價商品
        /// </summary>
        public bool DailyNewOnSale { get; set; } = false;

        /// <summary>
        /// 每日新上架最低價商品 (價格尾數為 7 的商品)
        /// </summary>
        public bool DailyNewBestBuy { get; set; } = false;

        /// <summary>
        /// 庫存通知商品 Code
        /// </summary>
        public List<string> InventoryCheckList { set; get; } = new();
    }
}
