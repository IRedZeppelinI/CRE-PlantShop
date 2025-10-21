using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlantShop.Domain.Entities;

namespace PlantShop.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        //ID sera de UserIdentity
        builder.Property(u => u.Id).ValueGeneratedNever();

        
        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.Address)
            .HasMaxLength(500); 

        
        builder.HasMany(u => u.Orders)
            .WithOne(o => o.Customer)    
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
