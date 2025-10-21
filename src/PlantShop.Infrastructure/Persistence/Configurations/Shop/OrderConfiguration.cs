using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlantShop.Domain.Entities.Shop;

namespace PlantShop.Infrastructure.Persistence.Configurations.Shop;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.OrderStatus)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(o => o.TotalPrice)
            .HasPrecision(18, 2);

        
        builder.HasMany(o => o.OrderItems)
            .WithOne(oi => oi.Order)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade); 
    }
}