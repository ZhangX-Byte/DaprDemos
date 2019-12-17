using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    public class DaprClientService : DaprClient.DaprClientBase
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

        public override Task<Any> OnInvoke(InvokeEnvelope request, ServerCallContext context)
        {
            Console.WriteLine(request.Data.Value.ToStringUtf8());
            switch (request.Method)
            {
                case "GetAllProducts":
                    Task<Any> productsList = GetAllProducts();
                    return productsList;
            }

            return GetAllProducts();
        }

        /// <summary>
        /// 获取所有产品
        /// </summary>
        /// <returns></returns>
        public async Task<Any> GetAllProducts()
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