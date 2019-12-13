using System;
using System.Linq;
using System.Threading.Tasks;
using Dapr.Client.Grpc;
using Google.Protobuf;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc;
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
            string defaultPort = Environment.GetEnvironmentVariable("DAPR_GRPC_PORT") ?? "54681";

            // Set correct switch to make insecure gRPC service calls. This switch must be set before creating the GrpcChannel.
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            // Create Client
            string daprUri = $"http://127.0.0.1:{defaultPort}";
            GrpcChannel channel = GrpcChannel.ForAddress(daprUri);
            var client = new Dapr.Client.Grpc.Dapr.DaprClient(channel);
            Console.WriteLine(daprUri);

            InvokeServiceResponseEnvelope result = await client.InvokeServiceAsync(new InvokeServiceEnvelope
            {
                Method = "MyMethod",
                Id = "client",
                Data = new Google.Protobuf.WellKnownTypes.Any
                {
                    Value = ByteString.CopyFromUtf8("Hello ProductService")
                }
            });
            Console.WriteLine("this is call result:" + result.Data.Value.ToStringUtf8());
            //var productResult = result.Data.Unpack<ProductList.V1.ProductList>();
            //Console.WriteLine("this is call result:" + productResult.Results.FirstOrDefault());
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