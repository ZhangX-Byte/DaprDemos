using System.Threading.Tasks;
using Daprclient;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace StorageService.Api.GrpcServices
{
    public sealed class DaprClientService : DaprClient.DaprClientBase
    {
        public override Task<GetTopicSubscriptionsEnvelope> GetTopicSubscriptions(Empty request, ServerCallContext context)
        {
            var topicSubscriptionsEnvelope = new GetTopicSubscriptionsEnvelope();
            topicSubscriptionsEnvelope.Topics.Add("Storage.Reduce");
            return Task.FromResult(topicSubscriptionsEnvelope);
        }
    }
}