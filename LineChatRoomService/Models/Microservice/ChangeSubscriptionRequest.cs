namespace LineChatRoomService.Models.Microservice
{
    public class ChangeSubscriptionRequest
    {
        public ChangeSubscriptionType requestType { get; set; }

        public string token { get; set; }

        public SubscriptionType? subscriptionType { get; set; }

        public string? code { get; set; }
    }

    public enum ChangeSubscriptionType
    {
        Unknown = 0,
        Create = 1,
        Delete = 2,
    }

    public enum SubscriptionType
    {
        Unknown = 0,
        DailyNewBestBuy = 1,
        DailyNewOnsale = 2,
        InventoryCheck = 3,
    }
}
