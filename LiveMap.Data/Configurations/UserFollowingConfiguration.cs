using LiveMap.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiveMap.Data.Configurations
{
    public class UserFollowingConfiguration : IEntityTypeConfiguration<UserFollowing>
    {
        public void Configure(EntityTypeBuilder<UserFollowing> builder)
        {
            builder.HasKey(uf => new { uf.UserId, uf.FolowingId });

            builder.HasOne(uf => uf.User)
                .WithMany(u => u.Followings)
                .HasForeignKey(uf => uf.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(uf => uf.Following)
                .WithMany(u => u.Followers)
                .HasForeignKey(uf => uf.FolowingId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
