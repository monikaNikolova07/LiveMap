using LiveMap.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiveMap.Data.Configurations
{
    public class FolderStructureConfiguration : IEntityTypeConfiguration<FolderStructure>
    {
        public void Configure(EntityTypeBuilder<FolderStructure> builder)
        {
            builder.HasKey(fs => new { fs.FolderId, fs.SubfolderId });

            builder.HasOne(fs => fs.Folder)
                .WithMany(f => f.Subfolders)
                .HasForeignKey(fs => fs.FolderId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(fs => fs.Subfolder)
                .WithMany(f => f.ParentFolders)
                .HasForeignKey(fs => fs.SubfolderId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
