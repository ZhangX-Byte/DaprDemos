using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapr;
using Microsoft.AspNetCore.Mvc;
using ProductService.Api.Entities;

namespace ProductService.Api.Controllers
{
    /// <summary>
    /// 产品集合.
    /// </summary>
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ProductContext _productContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProductsController" /> class.
        /// </summary>
        /// <param name="productContext">产品上下文.</param>
        public ProductsController(ProductContext productContext)
        {
            _productContext = productContext;
        }

        /// <summary>
        /// 获取产品列表.
        /// </summary>
        /// <returns>产品集合.</returns>
        [HttpGet("getlist")]
        public IList<Product> GetList()
        {
            return _productContext.Products.ToList();
        }

        /// <summary>
        /// 创建产品
        /// </summary>
        /// <param name="productCreate">产品创建模型</param>
        /// <returns></returns>
        [Topic("product"), HttpPost("product")]
        public async Task<bool> CreateProduct(ProductCreate productCreate)
        {
            _productContext.Products.Add(new Product
            {
                ProductID = productCreate.ID
            });
            return await _productContext.SaveChangesAsync() == 1;
        }
    }

    /// <summary>
    /// 产品创建模型.
    /// </summary>
    public class ProductCreate
    {
        /// <summary>
        /// Gets or sets 产品ID.
        /// </summary>
        public Guid ID { get; set; }
    }
}