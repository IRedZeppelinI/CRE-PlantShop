using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlantShop.Domain.Entities.Shop;

namespace PlantShop.Infrastructure.Persistence.Configurations.Shop;

public class ArticleConfiguration : IEntityTypeConfiguration<Article>
{
    public void Configure(EntityTypeBuilder<Article> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(200);

        
        builder.Property(a => a.Price)
            .HasPrecision(18, 2);

        builder.Property(a => a.ImageUrl)
            .HasMaxLength(2000); 
    }
}