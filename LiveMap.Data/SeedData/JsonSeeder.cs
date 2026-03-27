using System.Text.Json;
using LiveMap.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace LiveMap.Data.SeedData
{
    public class JsonSeeder
    {
        private readonly LiveMapDbContext _context;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly UserManager<User> _userManager;
        private readonly IHostEnvironment _environment;

        public JsonSeeder(
            LiveMapDbContext context,
            RoleManager<IdentityRole<Guid>> roleManager,
            UserManager<User> userManager,
            IHostEnvironment environment)
        {
            _context = context;
            _roleManager = roleManager;
            _userManager = userManager;
            _environment = environment;
        }

        public async Task SeedAsync()
        {
            await SeedRolesAsync();
            await SeedUsersAsync();
            await SeedUserRolesAsync();
            await SeedProfilesAsync();
            await SeedFoldersAsync();
            await SeedPicturesAsync();
            await SeedPictureLikesAsync();
            await SeedPictureCommentsAsync();
            await SeedFolderStructuresAsync();
            await SeedUserFollowingsAsync();
        }

        private async Task SeedRolesAsync()
        {
            var roles = await ReadSeedAsync<List<RoleSeedItem>>("roles.json") ?? new();

            foreach (var roleData in roles)
            {
                if (string.IsNullOrWhiteSpace(roleData.Name))
                {
                    continue;
                }

                var existingRole = await _roleManager.FindByNameAsync(roleData.Name);
                if (existingRole != null)
                {
                    continue;
                }

                var role = new IdentityRole<Guid>
                {
                    Id = ParseGuid(roleData.Id),
                    Name = roleData.Name,
                    NormalizedName = string.IsNullOrWhiteSpace(roleData.NormalizedName)
                        ? roleData.Name.ToUpperInvariant()
                        : roleData.NormalizedName
                };

                await _roleManager.CreateAsync(role);
            }
        }

        private async Task SeedUsersAsync()
        {
            var users = await ReadSeedAsync<List<UserSeedItem>>("users.json") ?? new();

            foreach (var userData in users)
            {
                if (string.IsNullOrWhiteSpace(userData.Email) || string.IsNullOrWhiteSpace(userData.Password))
                {
                    continue;
                }

                var existingUser = await _userManager.FindByEmailAsync(userData.Email);
                if (existingUser != null)
                {
                    continue;
                }

                var user = new User
                {
                    Id = ParseGuid(userData.Id),
                    UserName = string.IsNullOrWhiteSpace(userData.UserName) ? userData.Email : userData.UserName,
                    NormalizedUserName = (string.IsNullOrWhiteSpace(userData.UserName) ? userData.Email : userData.UserName).ToUpperInvariant(),
                    Email = userData.Email,
                    NormalizedEmail = userData.Email.ToUpperInvariant(),
                    EmailConfirmed = userData.EmailConfirmed,
                    FirstName = userData.FirstName,
                    LastName = userData.LastName,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                };

                await _userManager.CreateAsync(user, userData.Password);
            }
        }

        private async Task SeedUserRolesAsync()
        {
            var userRoles = await ReadSeedAsync<List<UserRoleSeedItem>>("userroles.json") ?? new();

            foreach (var userRole in userRoles)
            {
                var userId = ParseGuid(userRole.UserId);
                var roleId = ParseGuid(userRole.RoleId);

                var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);
                var role = await _roleManager.Roles.FirstOrDefaultAsync(r => r.Id == roleId);

                if (user == null || role == null || string.IsNullOrWhiteSpace(role.Name))
                {
                    continue;
                }

                if (await _userManager.IsInRoleAsync(user, role.Name))
                {
                    continue;
                }

                await _userManager.AddToRoleAsync(user, role.Name);
            }
        }

        private async Task SeedProfilesAsync()
        {
            var profiles = await ReadSeedAsync<List<ProfileSeedItem>>("profiles.json") ?? new();

            foreach (var profileData in profiles)
            {
                var profileId = ParseGuid(profileData.Id);
                if (await _context.Profiles.AnyAsync(p => p.Id == profileId))
                {
                    continue;
                }

                var userId = ParseGuid(profileData.UserId);
                if (!await _context.Users.AnyAsync(u => u.Id == userId))
                {
                    continue;
                }

                var profile = new Profile
                {
                    Id = profileId,
                    UserId = userId,
                    ProfilePicture = profileData.ProfilePicture,
                    Bio = profileData.Bio,
                    Acssesability = ParseAccessibility(profileData.Acssesability)
                };

                await _context.Profiles.AddAsync(profile);
            }

            await _context.SaveChangesAsync();
        }

        private async Task SeedFoldersAsync()
        {
            var folders = await ReadSeedAsync<List<FolderSeedItem>>("folders.json") ?? new();

            foreach (var folderData in folders)
            {
                var folderId = ParseGuid(folderData.Id);
                if (await _context.Folders.AnyAsync(f => f.Id == folderId))
                {
                    continue;
                }

                var profileId = ParseGuid(folderData.ProfileId);
                if (!await _context.Profiles.AnyAsync(p => p.Id == profileId))
                {
                    continue;
                }

                var folder = new Folder
                {
                    Id = folderId,
                    Name = folderData.Name,
                    ProfileId = profileId,
                    Acssesability = ParseAccessibility(folderData.Acssesability)
                };

                await _context.Folders.AddAsync(folder);
            }

            await _context.SaveChangesAsync();
        }

        private async Task SeedPicturesAsync()
        {
            var pictures = await ReadSeedAsync<List<PictureSeedItem>>("pictures.json") ?? new();

            foreach (var pictureData in pictures)
            {
                var pictureId = ParseGuid(pictureData.Id);
                if (await _context.Pictures.AnyAsync(p => p.Id == pictureId))
                {
                    continue;
                }

                var folderId = ParseGuid(pictureData.FolderId);
                if (!await _context.Folders.AnyAsync(f => f.Id == folderId))
                {
                    continue;
                }

                var picture = new Picture
                {
                    Id = pictureId,
                    URL = pictureData.Url,
                    FolderId = folderId,
                    Acssesability = ParseAccessibility(pictureData.Acssesability)
                };

                await _context.Pictures.AddAsync(picture);
            }

            await _context.SaveChangesAsync();
        }


        private async Task SeedPictureLikesAsync()
        {
            var pictureLikes = await ReadSeedAsync<List<PictureLikeSeedItem>>("picturelikes.json") ?? new();

            foreach (var item in pictureLikes)
            {
                var pictureId = ParseGuid(item.PictureId);
                var userId = ParseGuid(item.UserId);

                if (await _context.PictureLikes.AnyAsync(pl => pl.PictureId == pictureId && pl.UserId == userId))
                {
                    continue;
                }

                var picture = await _context.Pictures
                    .Include(p => p.Folder)
                        .ThenInclude(f => f.Profile)
                    .FirstOrDefaultAsync(p => p.Id == pictureId);

                if (picture == null || picture.Folder.Profile.UserId == userId || !await _context.Users.AnyAsync(u => u.Id == userId))
                {
                    continue;
                }

                await _context.PictureLikes.AddAsync(new PictureLike
                {
                    PictureId = pictureId,
                    UserId = userId
                });
            }

            await _context.SaveChangesAsync();
        }

        private async Task SeedPictureCommentsAsync()
        {
            var pictureComments = await ReadSeedAsync<List<PictureCommentSeedItem>>("picturecomments.json") ?? new();

            foreach (var item in pictureComments)
            {
                var commentId = ParseGuid(item.Id);
                if (await _context.PictureComments.AnyAsync(pc => pc.Id == commentId))
                {
                    continue;
                }

                var pictureId = ParseGuid(item.PictureId);
                var userId = ParseGuid(item.UserId);

                var picture = await _context.Pictures
                    .Include(p => p.Folder)
                        .ThenInclude(f => f.Profile)
                    .FirstOrDefaultAsync(p => p.Id == pictureId);

                if (picture == null || picture.Folder.Profile.UserId == userId || !await _context.Users.AnyAsync(u => u.Id == userId) || string.IsNullOrWhiteSpace(item.Content))
                {
                    continue;
                }

                await _context.PictureComments.AddAsync(new PictureComment
                {
                    Id = commentId,
                    PictureId = pictureId,
                    UserId = userId,
                    Content = item.Content.Trim()
                });
            }

            await _context.SaveChangesAsync();
        }

        private async Task SeedFolderStructuresAsync()
        {
            var folderStructures = await ReadSeedAsync<List<FolderStructureSeedItem>>("folderstructures.json") ?? new();

            foreach (var item in folderStructures)
            {
                var folderId = ParseGuid(item.FolderId);
                var subfolderId = ParseGuid(item.SubfolderId);

                if (await _context.FolderStructures.AnyAsync(fs => fs.FolderId == folderId && fs.SubfolderId == subfolderId))
                {
                    continue;
                }

                var folderExists = await _context.Folders.AnyAsync(f => f.Id == folderId);
                var subfolderExists = await _context.Folders.AnyAsync(f => f.Id == subfolderId);

                if (!folderExists || !subfolderExists)
                {
                    continue;
                }

                await _context.FolderStructures.AddAsync(new FolderStructure
                {
                    FolderId = folderId,
                    SubfolderId = subfolderId
                });
            }

            await _context.SaveChangesAsync();
        }

        private async Task SeedUserFollowingsAsync()
        {
            var userFollowings = await ReadSeedAsync<List<UserFollowingSeedItem>>("userfollowings.json") ?? new();

            foreach (var item in userFollowings)
            {
                var userId = ParseGuid(item.UserId);
                var followingId = ParseGuid(item.FolowingId);

                if (await _context.UserFollowings.AnyAsync(uf => uf.UserId == userId && uf.FolowingId == followingId))
                {
                    continue;
                }

                var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
                var followingExists = await _context.Users.AnyAsync(u => u.Id == followingId);

                if (!userExists || !followingExists)
                {
                    continue;
                }

                await _context.UserFollowings.AddAsync(new UserFollowing
                {
                    UserId = userId,
                    FolowingId = followingId
                });
            }

            await _context.SaveChangesAsync();
        }

        private async Task<T?> ReadSeedAsync<T>(string fileName)
        {
            var path = ResolveSeedFilePath(fileName);
            if (path == null)
            {
                return default;
            }

            var json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        private string? ResolveSeedFilePath(string fileName)
        {
            var candidates = new[]
            {
                Path.Combine(_environment.ContentRootPath, "..", "LiveMap.Data", "SeedData", "Json", fileName),
                Path.Combine(AppContext.BaseDirectory, "SeedData", "Json", fileName)
            };

            foreach (var candidate in candidates)
            {
                var fullPath = Path.GetFullPath(candidate);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            return null;
        }

        private static Guid ParseGuid(string value) => Guid.Parse(value);

        private static Acssesability ParseAccessibility(string? value)
        {
            return Enum.TryParse<Acssesability>(value, true, out var result)
                ? result
                : Acssesability.Public;
        }

        private class RoleSeedItem
        {
            public string Id { get; set; } = null!;
            public string Name { get; set; } = null!;
            public string? NormalizedName { get; set; }
        }

        private class UserSeedItem
        {
            public string Id { get; set; } = null!;
            public string? UserName { get; set; }
            public string Email { get; set; } = null!;
            public string Password { get; set; } = null!;
            public bool EmailConfirmed { get; set; } = true;
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
        }

        private class UserRoleSeedItem
        {
            public string UserId { get; set; } = null!;
            public string RoleId { get; set; } = null!;
        }

        private class ProfileSeedItem
        {
            public string Id { get; set; } = null!;
            public string UserId { get; set; } = null!;
            public string? ProfilePicture { get; set; }
            public string? Bio { get; set; }
            public string? Acssesability { get; set; }
        }

        private class FolderSeedItem
        {
            public string Id { get; set; } = null!;
            public string Name { get; set; } = null!;
            public string ProfileId { get; set; } = null!;
            public string? Acssesability { get; set; }
        }

        private class PictureSeedItem
        {
            public string Id { get; set; } = null!;
            public string Url { get; set; } = null!;
            public string FolderId { get; set; } = null!;
            public string? Acssesability { get; set; }
        }

        private class PictureLikeSeedItem
        {
            public string PictureId { get; set; } = null!;
            public string UserId { get; set; } = null!;
        }

        private class PictureCommentSeedItem
        {
            public string Id { get; set; } = null!;
            public string PictureId { get; set; } = null!;
            public string UserId { get; set; } = null!;
            public string Content { get; set; } = null!;
        }

        private class FolderStructureSeedItem
        {
            public string FolderId { get; set; } = null!;
            public string SubfolderId { get; set; } = null!;
        }

        private class UserFollowingSeedItem
        {
            public string UserId { get; set; } = null!;
            public string FolowingId { get; set; } = null!;
        }
    }
}
