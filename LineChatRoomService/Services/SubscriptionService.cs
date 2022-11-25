using LineChatRoomService.Models.Events.Intergration;
using LineChatRoomService.Models.Microservice;
using LineChatRoomService.Services.Interface;
using LineChatRoomService.Utility;
using System.Diagnostics.CodeAnalysis;

namespace LineChatRoomService.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly ILogger<SubscriptionService> _log;

        public IIntergrationEventService IntergrationService { get; }


        public SubscriptionService(ILogger<SubscriptionService> logger,
                                   IIntergrationEventService service)
        {
            this._log = logger;
            this.IntergrationService = service;
        }


        public async Task<bool> ChangeSubscription(ChangeSubscriptionType changeType, string token, SubscriptionType? subscriptionType, string? code)
        {
            if (subscriptionType == SubscriptionType.InventoryCheck && string.IsNullOrWhiteSpace(code))
                throw new Exception("Missing 'code'");

            var type = subscriptionType.ToString();


            IntergrationEvent @event;

            if (changeType == ChangeSubscriptionType.Create)
            {
                @event = new RegisterSubscription
                {
                    Code = code,
                    Subscriber = token,
                    SubscriberType = "LineNotify",
                    SubscriptionType = type,
                };

            }
            else if (changeType == ChangeSubscriptionType.Delete)
            {
                @event = new RemoveSubscription
                {
                    Code = code,
                    Subscriber = token,
                    SubscriberType = "LineNotify",
                    SubscriptionType = type,
                };
            }
            else
            {
                throw new Exception("Invalid ChangeSubscriptionType type");
            }

            await this.IntergrationService.PublishEvent(@event);

            return true;
        }

        public async Task<bool> DeleteAllSubscription([NotNull] string token)
        {

            var @event = new RemoveSubscriber
            {
                SubscriberType = "LineNotify",
                Subscriber = token,
            };

            await this.IntergrationService.PublishEvent(@event);

            return true;
        }
    }
}
