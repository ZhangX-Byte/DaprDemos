using System;
using System.Threading.Tasks;
using Daprclient;
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
            Console.WriteLine(request.Topic);
            Console.WriteLine(request.Id);
            Console.WriteLine(request.Source);
            Console.WriteLine(request.SpecVersion);
            Console.WriteLine(request.Type);
            if (request.Topic.Equals("Storage.Reduce"))
            {
                await HandlerStorageReduce(StorageReduceRequest.StorageReduceData.Parser.ParseFrom(request.Data.Value), context);
            }

            return new Empty();
        }

        public override Task<GetBindingsSubscriptionsEnvelope> GetBindingsSubscriptions(Empty request, ServerCallContext context)
        {
            var bindingsSubscriptionsEnvelope = new GetBindingsSubscriptionsEnvelope();
            return Task.FromResult(bindingsSubscriptionsEnvelope);
        }


        private async Task HandlerStorageReduce(StorageReduceRequest.StorageReduceData storageReduceData, ServerCallContext context)
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