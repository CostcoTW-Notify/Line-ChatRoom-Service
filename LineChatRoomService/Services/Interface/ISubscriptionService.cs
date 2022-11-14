using LineChatRoomService.Models.Microservice;

namespace LineChatRoomService.Services.Interface
{
    public interface ISubscriptionService
    {

        Task<bool> ChangeSubscription(ChangeSubscriptionType changeType, string token, SubscriptionType? subscriptionType, string? code);


        Task<bool> DeleteAllSubscription(string token);
    }
}
