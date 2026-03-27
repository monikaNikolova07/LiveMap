using LiveMap.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiveMap.Data.Configurations
{
    public class PictureConfiguration : IEntityTypeConfiguration<Picture>
    {
        public void Configure(EntityTypeBuilder<Picture> builder)
        {
            builder.HasKey(p => p.Id);

            builder.Property(p => p.URL)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(p => p.Description)
                .HasMaxLength(1000);

            builder.Property(p => p.Acssesability)
                .IsRequired();

            builder.Property(p => p.CreatedOn)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            builder.HasOne(p => p.Folder)
                .WithMany(f => f.Pictures)
                .HasForeignKey(p => p.FolderId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
