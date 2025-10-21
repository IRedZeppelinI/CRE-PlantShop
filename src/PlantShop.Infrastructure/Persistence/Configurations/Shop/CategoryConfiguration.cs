using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlantShop.Domain.Entities.Shop;

namespace PlantShop.Infrastructure.Persistence.Configurations.Shop;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Description)
            .HasMaxLength(500);

        
        builder.HasMany(c => c.Articles)
            .WithOne(a => a.Category)
            .HasForeignKey(a => a.CategoryId)
            .OnDelete(DeleteBehavior.Restrict); 
    }
}