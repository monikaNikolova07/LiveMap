using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using LiveMap.Core.Contracts;
using LiveMap.Core.DTOs.Admin;
using LiveMap.Data;
using LiveMap.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LiveMap.Core.Services
{
    public class AdminService : IAdminService
    {
        public const string AdminRoleName = "Admin";

        private readonly LiveMapDbContext context;
        private readonly UserManager<User> userManager;
        private readonly RoleManager<IdentityRole<Guid>> roleManager;
        private readonly Cloudinary cloudinary;

        public AdminService(
            LiveMapDbContext context,
            UserManager<User> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            Cloudinary cloudinary)
        {
            this.context = context;
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.cloudinary = cloudinary;
        }

        public async Task<AdminDashboardDto> GetDashboardAsync()
        {
            return new AdminDashboardDto
            {
                Profiles = await context.Profiles
                    .OrderBy(p => p.User.UserName)
                    .Select(p => new AdminProfileItemDto
                    {
                        Id = p.Id,
                        Username = p.User.UserName ?? string.Empty,
                        Email = p.User.Email,
                        Bio = p.Bio,
                        ProfilePicture = p.ProfilePicture,
                        FoldersCount = p.Folders.Count
                    })
                    .ToListAsync(),

                Folders = await context.Folders
                    .OrderBy(f => f.Name)
                    .Select(f => new AdminFolderItemDto
                    {
                        Id = f.Id,
                        Name = f.Name,
                        ProfileId = f.ProfileId,
                        Username = f.Profile.User.UserName ?? string.Empty,
                        ProfilePicture = f.Profile.ProfilePicture,
                        PicturesCount = f.Pictures.Count
                    })
                    .ToListAsync(),

                Photos = await context.Pictures
                    .Include(p => p.Folder)
                        .ThenInclude(f => f.Profile)
                            .ThenInclude(pr => pr.User)
                    .AsNoTracking()
                    .OrderByDescending(p => p.CreatedOn)
                    .ThenByDescending(p => p.Id)
                    .Select(p => new AdminPhotoItemDto
                    {
                        Id = p.Id,
                        Url = p.URL,
                        FolderId = p.FolderId,
                        FolderName = p.Folder.Name,
                        ProfileId = p.Folder.ProfileId,
                        Username = p.Folder.Profile.User.UserName ?? string.Empty,
                        CreatedOn = p.CreatedOn
                    })
                    .ToListAsync()
            };
        }

        public async Task EnsureRolesAndAdminsAsync(IEnumerable<string> adminEmails)
        {
            if (!await roleManager.RoleExistsAsync(AdminRoleName))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(AdminRoleName));
            }

            var normalizedEmails = adminEmails
                .Where(email => !string.IsNullOrWhiteSpace(email))
                .Select(email => email.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var email in normalizedEmails)
            {
                var user = await userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    continue;
                }

                if (!await userManager.IsInRoleAsync(user, AdminRoleName))
                {
                    await userManager.AddToRoleAsync(user, AdminRoleName);
                }
            }
        }

        public async Task DeleteProfileAsync(Guid profileId)
        {
            var profile = await context.Profiles
                .Include(p => p.Folders)
                    .ThenInclude(f => f.Pictures)
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == profileId);

            if (profile == null)
            {
                throw new InvalidOperationException("Profile not found.");
            }

            foreach (var folder in profile.Folders.ToList())
            {
                await DeleteFolderInternalAsync(folder);
            }

            if (!string.IsNullOrWhiteSpace(profile.ProfilePicture))
            {
                await DeleteCloudinaryAssetIfPossibleAsync(profile.ProfilePicture);
            }

            var userFollowings = await context.UserFollowings
                .Where(x => x.UserId == profile.UserId || x.FolowingId == profile.UserId)
                .ToListAsync();

            if (userFollowings.Count > 0)
            {
                context.UserFollowings.RemoveRange(userFollowings);
            }

            context.Profiles.Remove(profile);
            await context.SaveChangesAsync();

            if (profile.User != null)
            {
                await userManager.DeleteAsync(profile.User);
            }
        }

        public async Task DeleteFolderAsync(Guid folderId)
        {
            var folder = await context.Folders
                .Include(f => f.Pictures)
                .Include(f => f.Subfolders)
                .Include(f => f.ParentFolders)
                .FirstOrDefaultAsync(f => f.Id == folderId);

            if (folder == null)
            {
                throw new InvalidOperationException("Folder not found.");
            }

            await DeleteFolderInternalAsync(folder);
            await context.SaveChangesAsync();
        }

        public async Task DeletePhotoAsync(Guid photoId)
        {
            var photo = await context.Pictures.FirstOrDefaultAsync(p => p.Id == photoId);
            if (photo == null)
            {
                throw new InvalidOperationException("Photo not found.");
            }

            await DeletePhotoInternalAsync(photo);
            await context.SaveChangesAsync();
        }

        public async Task DeleteProfilePictureAsync(Guid profileId)
        {
            var profile = await context.Profiles.FirstOrDefaultAsync(p => p.Id == profileId);
            if (profile == null)
            {
                throw new InvalidOperationException("Profile not found.");
            }

            if (!string.IsNullOrWhiteSpace(profile.ProfilePicture))
            {
                await DeleteCloudinaryAssetIfPossibleAsync(profile.ProfilePicture);
                profile.ProfilePicture = string.Empty;
                await context.SaveChangesAsync();
            }
        }

        private async Task DeleteFolderInternalAsync(LiveMap.Data.Models.Folder folder)
        {
            if (folder.Pictures != null)
            {
                foreach (var photo in folder.Pictures.ToList())
                {
                    await DeletePhotoInternalAsync(photo);
                }
            }

            var subfolderLinks = await context.FolderStructures
                .Where(fs => fs.FolderId == folder.Id || fs.SubfolderId == folder.Id)
                .ToListAsync();

            if (subfolderLinks.Count > 0)
            {
                context.FolderStructures.RemoveRange(subfolderLinks);
            }

            context.Folders.Remove(folder);
        }

        private async Task DeletePhotoInternalAsync(Picture photo)
        {
            if (!string.IsNullOrWhiteSpace(photo.URL))
            {
                await DeleteCloudinaryAssetIfPossibleAsync(photo.URL);
            }

            context.Pictures.Remove(photo);
        }

        private async Task DeleteCloudinaryAssetIfPossibleAsync(string imageUrl)
        {
            var publicId = TryGetCloudinaryPublicId(imageUrl);
            if (string.IsNullOrWhiteSpace(publicId))
            {
                return;
            }

            try
            {
                await cloudinary.DestroyAsync(new DeletionParams(publicId)
                {
                    ResourceType = ResourceType.Image,
                    Invalidate = true
                });
            }
            catch
            {
                // Best-effort external cleanup. Database deletion still proceeds.
            }
        }

        private static string? TryGetCloudinaryPublicId(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl) || !Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri))
            {
                return null;
            }

            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList();
            var uploadIndex = segments.FindIndex(s => string.Equals(s, "upload", StringComparison.OrdinalIgnoreCase));
            if (uploadIndex < 0 || uploadIndex + 1 >= segments.Count)
            {
                return null;
            }

            var publicIdSegments = segments.Skip(uploadIndex + 1).ToList();
            if (publicIdSegments.Count == 0)
            {
                return null;
            }

            if (publicIdSegments[0].StartsWith("v", StringComparison.OrdinalIgnoreCase) && publicIdSegments[0].Length > 1 && publicIdSegments[0].Skip(1).All(char.IsDigit))
            {
                publicIdSegments.RemoveAt(0);
            }

            if (publicIdSegments.Count == 0)
            {
                return null;
            }

            var last = publicIdSegments[^1];
            var lastDotIndex = last.LastIndexOf('.');
            if (lastDotIndex > 0)
            {
                publicIdSegments[^1] = last[..lastDotIndex];
            }

            return string.Join('/', publicIdSegments);
        }
    }
}
