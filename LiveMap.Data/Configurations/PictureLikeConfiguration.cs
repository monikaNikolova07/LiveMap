using LiveMap.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiveMap.Data.Configurations
{
    public class PictureLikeConfiguration : IEntityTypeConfiguration<PictureLike>
    {
        public void Configure(EntityTypeBuilder<PictureLike> builder)
        {
            builder.HasKey(pl => new { pl.PictureId, pl.UserId });

            builder.Property(pl => pl.CreatedOn)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            builder.HasOne(pl => pl.Picture)
                .WithMany(p => p.Likes)
                .HasForeignKey(pl => pl.PictureId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(pl => pl.User)
                .WithMany(u => u.PictureLikes)
                .HasForeignKey(pl => pl.UserId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
