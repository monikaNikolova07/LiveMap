using LiveMap.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiveMap.Data.Configurations
{
    public class ProfileConfiguration : IEntityTypeConfiguration<Profile>
    {
        public void Configure(EntityTypeBuilder<Profile> builder)
        {
            builder.HasKey(p => p.Id);

            builder.Property(p => p.ProfilePicture)
                .HasMaxLength(500);

            builder.Property(p => p.Bio)
                .HasMaxLength(1000);

            builder.Property(p => p.Acssesability)
                .IsRequired();

            builder.HasOne(p => p.User)
                .WithOne(u => u.Profile)
                .HasForeignKey<Profile>(p => p.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasIndex(p => p.UserId)
                .IsUnique();
        }
    }
}
