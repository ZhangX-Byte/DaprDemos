using System;
using System.Linq;
using ProductService.Api.Entities;

namespace ProductService.Api.Utils
{
    public class DbSeed
    {
        public static void InitialProducts(ProductContext productContext)
        {
            if (productContext.Products.Any())
            {
                return;
            }

            for (var i = 0; i < 100; i++)
            {
                productContext.Products.Add(new Product
                {
                    ProductID = Guid.NewGuid()
                });
            }

            productContext.SaveChanges();
        }
    }
}