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

        public FolderService(LiveMapDbContext _context)
        {
            context = _context;
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

            var folder = new Folder
            {
                Id = Guid.NewGuid(),
                Name = model.Name,
                ProfileId = profile.Id,
                Acssesability = model.Acssesability
            };

            await context.Folders.AddAsync(folder);
            await context.SaveChangesAsync();
        }

        public async Task<Folder?> GetByIdAsync(Guid id)
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

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var picture = new Picture
            {
                Id = Guid.NewGuid(),
                FolderId = folderId,
                URL = "/uploads/" + uniqueFileName,
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
