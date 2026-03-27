using LiveMap.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LiveMap.Data
{
    public class LiveMapDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
    {
        public LiveMapDbContext(DbContextOptions<LiveMapDbContext> options) : base(options)
        {
        }

        public DbSet<Folder> Folders { get; set; } = null!;
        public DbSet<FolderStructure> FolderStructures { get; set; } = null!;
        public DbSet<Picture> Pictures { get; set; } = null!;
        public DbSet<PictureLike> PictureLikes { get; set; } = null!;
        public DbSet<PictureComment> PictureComments { get; set; } = null!;
        public DbSet<Profile> Profiles { get; set; } = null!;
        public DbSet<UserFollowing> UserFollowings { get; set; } = null!;
        public DbSet<UserCountryMapColor> UserCountryMapColors { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ApplyConfigurationsFromAssembly(typeof(LiveMapDbContext).Assembly);
        }
    }
}
