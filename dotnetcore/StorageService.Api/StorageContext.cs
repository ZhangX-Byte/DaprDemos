using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using StorageService.Api.Entities;

namespace StorageService.Api
{
    public class StorageContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StorageContext" /> class.
        /// </summary>
        /// <param name="options">ProductContext Options.</param>
        public StorageContext(DbContextOptions<StorageContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Gets or sets 仓库集合.
        /// </summary>
        public DbSet<Storage> Storage { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            IEnumerable<Type> typesToRegister = Assembly.GetExecutingAssembly().GetTypes().Where(q => q.GetInterface(typeof(IEntityTypeConfiguration<>).FullName) != null);

            foreach (Type type in typesToRegister)
            {
                dynamic configurationInstance = Activator.CreateInstance(type);
                modelBuilder.ApplyConfiguration(configurationInstance);
            }
        }
    }
}