using System;
using System.Threading.Tasks;
using Daprclient;
using Daprexamples;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using StorageService.Api.Entities;

namespace StorageService.Api.GrpcServices
{
    public sealed class DaprClientService : DaprClient.DaprClientBase
    {
        private readonly StorageContext _storageContext;

        public DaprClientService(StorageContext storageContext)
        {
            _storageContext = storageContext;
        }

        public override Task<GetTopicSubscriptionsEnvelope> GetTopicSubscriptions(Empty request, ServerCallContext context)
        {
            var topicSubscriptionsEnvelope = new GetTopicSubscriptionsEnvelope();
            topicSubscriptionsEnvelope.Topics.Add("Storage.Reduce");
            return Task.FromResult(topicSubscriptionsEnvelope);
        }

        public override async Task<Empty> OnTopicEvent(CloudEventEnvelope request, ServerCallContext context)
        {
            if (request.Topic.Equals("Storage.Reduce"))
            {
                StorageReduceData storageReduceData = StorageReduceData.Parser.ParseFrom(request.Data.Value);
                Console.WriteLine("ProductID:" + storageReduceData.ProductID);
                Console.WriteLine("Amount:" + storageReduceData.Amount);
                await HandlerStorageReduce(storageReduceData);
            }
            return new Empty();
        }

        private async Task HandlerStorageReduce(StorageReduceData storageReduceData)
        {
            Guid productID = Guid.Parse(storageReduceData.ProductID);
            Storage storageFromDb = await _storageContext.Storage.FirstOrDefaultAsync(q => q.ProductID.Equals(productID));
            if (storageFromDb == null)
            {
                return;
            }

            if (storageFromDb.Amount < storageReduceData.Amount)
            {
                return;
            }

            storageFromDb.Amount -= storageReduceData.Amount;
            Console.WriteLine(storageFromDb.Amount);
            await _storageContext.SaveChangesAsync();
        }

    }
}