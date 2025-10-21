using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlantShop.Domain.Entities.Shop;

namespace PlantShop.Infrastructure.Persistence.Configurations.Shop;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.HasKey(oi => oi.Id);

        builder.Property(oi => oi.UnitPrice)
            .HasPrecision(18, 2);

        
        builder.HasOne(oi => oi.Article)
            .WithMany() 
            .HasForeignKey(oi => oi.ArticleId)
            .OnDelete(DeleteBehavior.Restrict); 
    }
}