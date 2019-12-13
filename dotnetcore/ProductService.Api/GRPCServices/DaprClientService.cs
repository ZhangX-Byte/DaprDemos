using System.Collections.Generic;
using System.Threading.Tasks;
using Dapr.Client.Grpc;
using Daprclient;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using ProductService.Api.Entities;

namespace ProductService.Api.GRPCServices
{
    /// <summary>
    /// Dapr Client Service Implement.
    /// </summary>
    public sealed class DaprClientService : DaprClient.DaprClientClient
    {
        private readonly ProductContext _productContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProductService" /> class.
        /// </summary>
        /// <param name="productContext">productContext.</param>
        public DaprClientService(ProductContext productContext)
        {
            _productContext = productContext;
        }

        public override AsyncUnaryCall<Any> OnInvokeAsync(InvokeEnvelope request, CallOptions options)
        {
            switch (request.Method)
            {
                case "GetAllProducts":
                    Task<Any> productsList = GetAllProducts();
                    return new AsyncUnaryCall<Any>(productsList, Task.FromResult(new Metadata()), () => Status.DefaultSuccess, () => new Metadata(), () => { });
            }

            return null;
        }

        public override Any OnInvoke(InvokeEnvelope request, CallOptions options)
        {
            switch (request.Method)
            {
                case "GetAllProducts":
                    Any productsList = GetAllProducts().GetAwaiter().GetResult();
                    return productsList;
            }

            return null;
        }

        /// <summary>
        /// 获取所有产品
        /// </summary>
        /// <returns></returns>
        private async Task<Any> GetAllProducts()
        {
            IList<Product> results = await _productContext.Products.ToListAsync();
            var productList = new ProductList.V1.ProductList();
            foreach (Product item in results)
            {
                productList.Results.Add(new ProductList.V1.Product
                {
                    ID = item.ProductID.ToString()
                });
            }

            return Any.Pack(productList);
        }
    }
}