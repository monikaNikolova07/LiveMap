using System.Text.Json;
using LiveMap.Data.Models;
using LiveMap.Data.SeedData.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LiveMap.Data.SeedData
{
    public class JsonSeeder : IJsonSeeder
    {
        private readonly LiveMapDbContext _context;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly UserManager<User> _userManager;
        private readonly string _jsonFolderPath;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public JsonSeeder(
            LiveMapDbContext context,
            RoleManager<IdentityRole<Guid>> roleManager,
            UserManager<User> userManager)
        {
            _context = context;
            _roleManager = roleManager;
            _userManager = userManager;
            _jsonFolderPath = Path.Combine(AppContext.BaseDirectory, "SeedData", "Json");
        }

        public async Task SeedAsync()
        {
            await SeedRolesAsync();
            await SeedUsersAsync();
            await SeedProfilesAsync();
            await SeedFoldersAsync();
            await SeedPicturesAsync();
        }

        private async Task SeedRolesAsync()
        {
            var roles = await ReadJsonAsync<List<RoleSeedModel>>("roles.json");
            if (roles == null || roles.Count == 0)
            {
                return;
            }

            foreach (var roleData in roles)
            {
                if (await _roleManager.RoleExistsAsync(roleData.Name))
                {
                    continue;
                }

                await _roleManager.CreateAsync(new IdentityRole<Guid>
                {
                    Id = Guid.Parse(roleData.Id),
                    Name = roleData.Name,
                    NormalizedName = roleData.NormalizedName
                });
            }
        }

        private async Task SeedUsersAsync()
        {
            var users = await ReadJsonAsync<List<UserSeedModel>>("users.json");
            if (users == null || users.Count == 0)
            {
                return;
            }

            foreach (var userData in users)
            {
                var existingUser = await _userManager.FindByEmailAsync(userData.Email);
                if (existingUser != null)
                {
                    continue;
                }

                var user = new User
                {
                    Id = Guid.Parse(userData.Id),
                    UserName = userData.Email,
                    NormalizedUserName = userData.Email.ToUpperInvariant(),
                    Email = userData.Email,
                    NormalizedEmail = userData.Email.ToUpperInvariant(),
                    EmailConfirmed = true,
                    FirstName = userData.FirstName,
                    LastName = userData.LastName
                };

                var result = await _userManager.CreateAsync(user, userData.Password);
                if (!result.Succeeded)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(userData.Role) && await _roleManager.RoleExistsAsync(userData.Role))
                {
                    await _userManager.AddToRoleAsync(user, userData.Role);
                }
            }
        }

        private async Task SeedProfilesAsync()
        {
            if (await _context.Profiles.AnyAsync())
            {
                return;
            }

            var profiles = await ReadJsonAsync<List<Profile>>("profiles.json");
            if (profiles == null || profiles.Count == 0)
            {
                return;
            }

            await _context.Profiles.AddRangeAsync(profiles);
            await _context.SaveChangesAsync();
        }

        private async Task SeedFoldersAsync()
        {
            if (await _context.Folders.AnyAsync())
            {
                return;
            }

            var folders = await ReadJsonAsync<List<Folder>>("folders.json");
            if (folders == null || folders.Count == 0)
            {
                return;
            }

            await _context.Folders.AddRangeAsync(folders);
            await _context.SaveChangesAsync();
        }

        private async Task SeedPicturesAsync()
        {
            if (await _context.Pictures.AnyAsync())
            {
                return;
            }

            var pictures = await ReadJsonAsync<List<Picture>>("pictures.json");
            if (pictures == null || pictures.Count == 0)
            {
                return;
            }

            await _context.Pictures.AddRangeAsync(pictures);
            await _context.SaveChangesAsync();
        }

        private async Task<T?> ReadJsonAsync<T>(string fileName)
        {
            var path = Path.Combine(_jsonFolderPath, fileName);
            if (!File.Exists(path))
            {
                return default;
            }

            var json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
    }
}
