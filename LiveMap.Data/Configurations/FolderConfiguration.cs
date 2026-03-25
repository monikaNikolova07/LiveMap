using LiveMap.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiveMap.Data.Configurations
{
    public class FolderConfiguration : IEntityTypeConfiguration<Folder>
    {
        public void Configure(EntityTypeBuilder<Folder> builder)
        {
            builder.HasKey(f => f.Id);

            builder.Property(f => f.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(f => f.Acssesability)
                .IsRequired();

            builder.HasOne(f => f.Profile)
                .WithMany(p => p.Folders)
                .HasForeignKey(f => f.ProfileId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasMany(f => f.Pictures)
                .WithOne(p => p.Folder)
                .HasForeignKey(p => p.FolderId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
