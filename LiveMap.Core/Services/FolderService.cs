using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using LiveMap.Core.Contracts;
using LiveMap.Core.DTOs.Folders;
using LiveMap.Data;
using LiveMap.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace LiveMap.Core.Services
{
    public class FolderService : IFolderService
    {
        private readonly LiveMapDbContext context;
        private readonly Cloudinary cloudinary;

        public FolderService(LiveMapDbContext _context, Cloudinary _cloudinary)
        {
            context = _context;
            cloudinary = _cloudinary;
        }

        public async Task<IEnumerable<FolderIndexDto>> GetAllAsync()
        {
            return await context.Folders
                .Select(f => new FolderIndexDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    ProfileId = f.ProfileId,
                    ProfilePicture = f.Profile != null ? f.Profile.ProfilePicture : string.Empty,
                    PicturesCount = f.Pictures.Count,
                    Acssesability = f.Acssesability
                })
                .ToListAsync();
        }

        public async Task CreateAsync(FolderCreateDto model, Guid userId)
        {
            var profile = await context.Users
                .Include(u => u.Profile)
                .Where(u => u.Id == userId)
                .Select(u => u.Profile)
                .FirstOrDefaultAsync();

            if (profile == null)
            {
                throw new InvalidOperationException("Profile not found for the current user.");
            }

            var folder = new LiveMap.Data.Models.Folder
            {
                Id = Guid.NewGuid(),
                Name = model.Name,
                ProfileId = profile.Id,
                Acssesability = model.Acssesability
            };

            await context.Folders.AddAsync(folder);
            await context.SaveChangesAsync();
        }

        public async Task<LiveMap.Data.Models.Folder?> GetByIdAsync(Guid id)
        {
            return await context.Folders
                .Include(f => f.Pictures)
                .Include(f => f.Profile)
                .FirstOrDefaultAsync(f => f.Id == id);
        }

        public async Task<IEnumerable<Picture>> GetPicturesAsync(Guid folderId)
        {
            return await context.Pictures
                .Where(p => p.FolderId == folderId)
                .ToListAsync();
        }

        public async Task UploadPictureAsync(Guid folderId, IFormFile file)
        {
            var folder = await context.Folders.FirstOrDefaultAsync(f => f.Id == folderId);
            if (folder == null)
            {
                throw new InvalidOperationException("Folder not found.");
            }

            if (file == null || file.Length == 0)
            {
                throw new InvalidOperationException("No file selected.");
            }

            if (!file.ContentType.StartsWith("image/"))
            {
                throw new InvalidOperationException("Only image files are allowed.");
            }

            await using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = $"livemap/{folderId}",
                PublicId = Guid.NewGuid().ToString(),
                UseFilename = true,
                UniqueFilename = true,
                Overwrite = false
            };

            var uploadResult = await cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                throw new InvalidOperationException(uploadResult.Error.Message);
            }

            var picture = new Picture
            {
                Id = Guid.NewGuid(),
                FolderId = folderId,
                URL = uploadResult.SecureUrl?.ToString() ?? string.Empty,
                Acssesability = Acssesability.Public
            };

            await context.Pictures.AddAsync(picture);
            await context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var folder = await context.Folders
                .Include(f => f.Pictures)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (folder == null)
            {
                throw new InvalidOperationException("Folder not found.");
            }

            context.Pictures.RemoveRange(folder.Pictures);
            context.Folders.Remove(folder);

            await context.SaveChangesAsync();
        }
    }
}
