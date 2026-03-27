using LiveMap.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiveMap.Data.Configurations
{
    public class UserCountryMapColorConfiguration : IEntityTypeConfiguration<UserCountryMapColor>
    {
        public void Configure(EntityTypeBuilder<UserCountryMapColor> builder)
        {
            builder.HasIndex(x => new { x.UserId, x.Country }).IsUnique();

            builder.Property(x => x.Country)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.ColorHex)
                .IsRequired()
                .HasMaxLength(20);
        }
    }
}
