using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlantShop.Domain.Entities;

namespace PlantShop.Infrastructure.Persistence.Configurations;

//conf de app user à parte por entidade utilizar Identity
public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
                
        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.Address)
            .HasMaxLength(500); 

        
        builder.HasMany(u => u.Orders)
            .WithOne(o => o.User)    
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
