using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using LineChatRoomService.Models.Events.Intergration;
using LineChatRoomService.Services.Interface;
using System.Text.Json;

namespace LineChatRoomService.Services
{
    public class IntergrationEventService : IIntergrationEventService
    {
        public PublisherClient Publisher { get; }

        public IntergrationEventService(PublisherClient publisher)
        {
            this.Publisher = publisher;
        }

        public async Task PublishEvent(IntergrationEvent @event)
        {
            var eventType = @event.GetType().Name;

            var json = JsonSerializer.Serialize(@event, @event.GetType());
            var data = ByteString.CopyFromUtf8(json);

            await this.Publisher.PublishAsync(new PubsubMessage
            {
                Data = data,
                Attributes =
                {
                    { "eventType" , eventType},
                    { "application","LineChatRoomService"}
                }
            });
        }
    }
}
