using System;
using System.Linq;
using System.Threading.Tasks;
using Dapr.Client.Grpc;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc;
using ProductList.V1;
using StorageService.Api.Entities;

namespace StorageService.Api.Controllers
{
    [ApiController]
    public class StorageController : ControllerBase
    {
        private readonly StorageContext _storageContext;

        public StorageController(StorageContext storageContext)
        {
            _storageContext = storageContext;
        }

        /// <summary>
        /// 初始化仓库.
        /// </summary>
        /// <returns>是否成功.</returns>
        [HttpGet("InitialStorage")]
        public async Task<bool> InitialStorage()
        {
            string defaultPort = Environment.GetEnvironmentVariable("DAPR_GRPC_PORT") ?? "5001";

            // Set correct switch to make insecure gRPC service calls. This switch must be set before creating the GrpcChannel.
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            // Create Client
            string daprUri = $"http://127.0.0.1:{defaultPort}";
            GrpcChannel channel = GrpcChannel.ForAddress(daprUri);
            var client = new Dapr.Client.Grpc.Dapr.DaprClient(channel);

            InvokeServiceResponseEnvelope result = await client.InvokeServiceAsync(new InvokeServiceEnvelope
            {
                Method = "GetAllProducts",
                Id = "productService",
                Data = Any.Pack(new ProductListRequest())
            });
            ProductList.V1.ProductList productResult = ProductList.V1.ProductList.Parser.ParseFrom(result.Data.Value);

            var random = new Random();

            foreach (Product item in productResult.Results)
            {
                _storageContext.Storage.Add(new Storage
                {
                    ProductID = Guid.Parse(item.ID),
                    Amount = random.Next(1, 1000)
                });
            }

            await _storageContext.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// 修改库存
        /// </summary>
        /// <param name="storage"></param>
        /// <returns></returns>
        [HttpPut("Reduce")]
        public bool Reduce(Storage storage)
        {
            Storage storageFromDb = _storageContext.Storage.FirstOrDefault(q => q.ProductID.Equals(storage.ProductID));
            if (storageFromDb == null)
            {
                return false;
            }

            if (storageFromDb.Amount <= storage.Amount)
            {
                return false;
            }

            storageFromDb.Amount -= storage.Amount;
            return true;
        }
    }
}