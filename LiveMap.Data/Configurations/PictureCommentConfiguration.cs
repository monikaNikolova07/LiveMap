using LiveMap.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiveMap.Data.Configurations
{
    public class PictureCommentConfiguration : IEntityTypeConfiguration<PictureComment>
    {
        public void Configure(EntityTypeBuilder<PictureComment> builder)
        {
            builder.HasKey(pc => pc.Id);

            builder.Property(pc => pc.Content)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(pc => pc.CreatedOn)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            builder.HasOne(pc => pc.Picture)
                .WithMany(p => p.Comments)
                .HasForeignKey(pc => pc.PictureId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(pc => pc.User)
                .WithMany(u => u.PictureComments)
                .HasForeignKey(pc => pc.UserId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
