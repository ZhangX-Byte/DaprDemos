using System.Threading.Tasks;
using Daprclient;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using ProductList.V1;

namespace ProductService.Api.GRPCServices
{
    /// <summary>
    /// Dapr Client Service Implement.
    /// </summary>
    public class DaprClientService : DaprClient.DaprClientBase
    {
        private readonly ProductListService _productListService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProductService" /> class.
        /// </summary>
        /// <param name="productListService"></param>
        public DaprClientService(ProductListService productListService)
        {
            _productListService = productListService;
        }

        public override async Task<Any> OnInvoke(InvokeEnvelope request, ServerCallContext context)
        {
            switch (request.Method)
            {
                case "GetAllProducts":
                    ProductListRequest productListRequest = ProductListRequest.Parser.ParseFrom(request.Data.Value);
                    ProductList.V1.ProductList productsList = await _productListService.GetAllProducts(productListRequest, context);
                    return Any.Pack(productsList);
            }
            return null;
        }
    }
}