using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using ProductList.V1;
using Product = ProductService.Api.Entities.Product;

namespace ProductService.Api.GRPCServices
{
    public class ProductListService : ProductRPCService.ProductRPCServiceBase
    {
        private readonly ProductContext _productContext;

        public ProductListService(ProductContext productContext)
        {
            _productContext = productContext;
        }

        public override async Task<ProductList.V1.ProductList> GetAllProducts(ProductListRequest request, ServerCallContext context)
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

            return productList;
        }
    }
}