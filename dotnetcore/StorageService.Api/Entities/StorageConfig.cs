using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StorageService.Api.Entities
{
    public class StorageConfig : IEntityTypeConfiguration<Storage>
    {
        public void Configure(EntityTypeBuilder<Storage> builder)
        {
            builder.HasNoKey();
            builder.Property(q => q.ProductID).IsRequired();
            builder.Property(q => q.Amount).IsRequired();
        }
    }
}