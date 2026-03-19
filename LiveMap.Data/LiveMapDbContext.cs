using LiveMap.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace LiveMap.Data
{
    public class LiveMapDbContext 
        : IdentityDbContext<User,IdentityRole<Guid>, Guid>
    {

        public LiveMapDbContext(DbContextOptions<LiveMapDbContext> options) : base(options)
        {

        }
        public LiveMapDbContext()
        {

        }

        public DbSet<Folder> Folders { get; set; }
        public DbSet<FolderStructure> FolderStructures { get; set; }
        public DbSet<Picture> Pictures { get; set; }
        public DbSet<Profile> Profiles { get; set; }
        public DbSet<UserFollowing> UserFollowings { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder builder)
        {
            base.OnConfiguring(builder);
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            foreach (var foreignKey in builder.Model.GetEntityTypes()
             .SelectMany(e => e.GetForeignKeys()))
            {
                foreignKey.DeleteBehavior = DeleteBehavior.NoAction;
            }

            builder.Entity<User>()
                .HasOne(u => u.Profile)
                .WithOne(p => p.User)
                .HasForeignKey<Profile>(p => p.UserId)
                .OnDelete(DeleteBehavior.NoAction);
        }

    }
}
