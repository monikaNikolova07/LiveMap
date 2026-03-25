using LiveMap.Core.Contracts;
using LiveMap.Core.DTOs.Profiles;
using LiveMap.Data;
using Microsoft.EntityFrameworkCore;

namespace LiveMap.Core.Services
{
    public class ProfileService : IProfileService
    {
        private readonly LiveMapDbContext context;

        public ProfileService(LiveMapDbContext context)
        {
            this.context = context;
        }

        public async Task<ProfileIndexDto?> GetProfileAsync(string userId)
        {
            if (!Guid.TryParse(userId, out var userGuid))
            {
                return null;
            }

            var profile = await context.Profiles
                .Where(p => p.UserId == userGuid)
                .Select(p => new ProfileIndexDto
                {
                    Id = p.Id,
                    ProfilePicture = p.ProfilePicture,
                    Bio = p.Bio,
                    Username = p.User.UserName,
                    Folders = p.Folders.Select(f => new ProfileFolderDto
                    {
                        Id = f.Id,
                        Name = f.Name
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            return profile;
        }

        public async Task<ProfileEditDto?> GetProfileForEditAsync(string userId)
        {
            if (!Guid.TryParse(userId, out var userGuid))
            {
                return null;
            }

            var profile = await context.Profiles
                .Where(p => p.UserId == userGuid)
                .Select(p => new ProfileEditDto
                {
                    Id = p.Id,
                    Username = p.User.UserName ?? string.Empty,
                    Bio = p.Bio ?? string.Empty,
                    ProfilePicture = p.ProfilePicture ?? string.Empty
                })
                .FirstOrDefaultAsync();

            return profile;
        }

        public async Task EditProfileAsync(ProfileEditDto dto, string userId)
        {
            if (!Guid.TryParse(userId, out var userGuid))
            {
                return;
            }

            var profile = await context.Profiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == userGuid);

            if (profile == null)
            {
                return;
            }

            profile.Bio = dto.Bio;
            profile.ProfilePicture = dto.ProfilePicture;

            if (profile.User != null)
            {
                profile.User.UserName = dto.Username;
                profile.User.NormalizedUserName = dto.Username.ToUpperInvariant();
            }

            await context.SaveChangesAsync();
        }
    }
}
