namespace ProductService.Api
{
    using Microsoft.EntityFrameworkCore;
    using ProductService.Api.Entities;

    /// <summary>
    /// 产品上下文.
    /// </summary>
    public sealed class ProductContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProductContext"/> class.
        /// </summary>
        /// <param name="options">ProductContext Options.</param>
        public ProductContext(DbContextOptions<ProductContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Gets or sets 产品集合.
        /// </summary>
        public DbSet<Product> Products { get; set; }
    }
}