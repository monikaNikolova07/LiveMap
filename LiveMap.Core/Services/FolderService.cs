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

        public FolderService(LiveMapDbContext context, Cloudinary cloudinary)
        {
            this.context = context;
            this.cloudinary = cloudinary;
        }

        public async Task<IEnumerable<FolderIndexDto>> GetAllAsync(Guid? currentUserId = null)
        {
            var query = context.Folders.AsQueryable();

            if (currentUserId.HasValue)
            {
                query = query.Where(f => f.Profile.UserId == currentUserId.Value);
            }

            return await query
                .OrderBy(f => f.Name)
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

        public async Task<LiveMap.Data.Models.Folder> CreateAsync(FolderCreateDto model, Guid userId)
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

            LiveMap.Data.Models.Folder? parentFolder = null;
            if (model.ParentFolderId.HasValue)
            {
                parentFolder = await context.Folders
                    .Include(f => f.Profile)
                    .FirstOrDefaultAsync(f => f.Id == model.ParentFolderId.Value);

                if (parentFolder == null)
                {
                    throw new InvalidOperationException("Parent folder not found.");
                }

                if (parentFolder.ProfileId != profile.Id)
                {
                    throw new InvalidOperationException("You can only create subfolders in your own folders.");
                }
            }

            var effectiveAccessibility = await GetEffectiveCreateAccessibilityAsync(model.ParentFolderId, model.Acssesability);

            var folder = new LiveMap.Data.Models.Folder
            {
                Id = Guid.NewGuid(),
                Name = model.Name,
                ProfileId = profile.Id,
                Acssesability = effectiveAccessibility
            };

            await context.Folders.AddAsync(folder);

            if (parentFolder != null)
            {
                await context.FolderStructures.AddAsync(new FolderStructure
                {
                    FolderId = parentFolder.Id,
                    SubfolderId = folder.Id
                });
            }

            await context.SaveChangesAsync();
            return folder;
        }



        public async Task<IEnumerable<Acssesability>> GetAvailableAccessibilitiesForCreateAsync(Guid? parentFolderId)
        {
            if (!parentFolderId.HasValue)
            {
                return Enum.GetValues<Acssesability>();
            }

            var rootAccessibility = await GetRootFolderAccessibilityAsync(parentFolderId.Value);
            if (rootAccessibility == Acssesability.Private)
            {
                return new[] { Acssesability.Private };
            }

            return Enum.GetValues<Acssesability>();
        }

        public async Task<Acssesability> GetUploadAccessibilityAsync(Guid folderId, Acssesability requestedAccessibility)
        {
            var rootAccessibility = await GetRootFolderAccessibilityAsync(folderId);
            if (rootAccessibility == Acssesability.Private)
            {
                return Acssesability.Private;
            }

            var folder = await context.Folders.FirstOrDefaultAsync(f => f.Id == folderId);
            if (folder == null)
            {
                throw new InvalidOperationException("Folder not found.");
            }

            if (folder.Acssesability == Acssesability.Private)
            {
                return Acssesability.Private;
            }

            return requestedAccessibility;
        }

        private async Task<Acssesability> GetEffectiveCreateAccessibilityAsync(Guid? parentFolderId, Acssesability requestedAccessibility)
        {
            if (!parentFolderId.HasValue)
            {
                return requestedAccessibility;
            }

            var rootAccessibility = await GetRootFolderAccessibilityAsync(parentFolderId.Value);
            return rootAccessibility == Acssesability.Private ? Acssesability.Private : requestedAccessibility;
        }

        private async Task<Acssesability> GetRootFolderAccessibilityAsync(Guid folderId)
        {
            var currentFolderId = folderId;

            while (true)
            {
                var folder = await context.Folders
                    .Include(f => f.ParentFolders)
                    .FirstOrDefaultAsync(f => f.Id == currentFolderId);

                if (folder == null)
                {
                    throw new InvalidOperationException("Folder not found.");
                }

                var parentLink = folder.ParentFolders.FirstOrDefault();
                if (parentLink == null)
                {
                    return folder.Acssesability;
                }

                currentFolderId = parentLink.FolderId;
            }
        }

        public async Task<LiveMap.Data.Models.Folder?> GetByIdAsync(Guid id)
        {
            return await context.Folders
                .Include(f => f.Profile)
                    .ThenInclude(p => p.User)
                .Include(f => f.Pictures)
                .Include(f => f.Subfolders)
                    .ThenInclude(fs => fs.Subfolder)
                        .ThenInclude(sf => sf.Pictures)
                .Include(f => f.Subfolders)
                    .ThenInclude(fs => fs.Subfolder)
                        .ThenInclude(sf => sf.Subfolders)
                .FirstOrDefaultAsync(f => f.Id == id);
        }

        public async Task<IEnumerable<Picture>> GetPicturesAsync(Guid folderId)
        {
            return await context.Pictures
                .Where(p => p.FolderId == folderId)
                .ToListAsync();
        }

        public async Task UploadPictureAsync(Guid folderId, IFormFile file, Acssesability acssesability)
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
                Acssesability = await GetUploadAccessibilityAsync(folderId, acssesability),
                CreatedOn = DateTime.UtcNow
            };

            await context.Pictures.AddAsync(picture);
            await context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var foldersToDelete = new List<LiveMap.Data.Models.Folder>();
            await CollectFoldersForDeleteAsync(id, foldersToDelete);

            if (!foldersToDelete.Any())
            {
                throw new InvalidOperationException("Folder not found.");
            }

            var folderIds = foldersToDelete.Select(f => f.Id).ToList();

            var pictures = await context.Pictures
                .Where(p => folderIds.Contains(p.FolderId))
                .ToListAsync();

            var structures = await context.FolderStructures
                .Where(fs => folderIds.Contains(fs.FolderId) || folderIds.Contains(fs.SubfolderId))
                .ToListAsync();

            context.Pictures.RemoveRange(pictures);
            context.FolderStructures.RemoveRange(structures);
            context.Folders.RemoveRange(foldersToDelete.GroupBy(f => f.Id).Select(g => g.First()));

            await context.SaveChangesAsync();
        }

        private async Task CollectFoldersForDeleteAsync(Guid folderId, List<LiveMap.Data.Models.Folder> foldersToDelete)
        {
            var folder = await context.Folders
                .Include(f => f.Subfolders)
                .FirstOrDefaultAsync(f => f.Id == folderId);

            if (folder == null || foldersToDelete.Any(f => f.Id == folderId))
            {
                return;
            }

            foldersToDelete.Add(folder);

            foreach (var subfolder in folder.Subfolders)
            {
                await CollectFoldersForDeleteAsync(subfolder.SubfolderId, foldersToDelete);
            }
        }

        public async Task UpdateAsync(Guid folderId, string name, Acssesability acssesability)
        {
            var folder = await context.Folders
                .FirstOrDefaultAsync(f => f.Id == folderId);

            if (folder == null)
            {
                throw new InvalidOperationException("Folder not found.");
            }

            folder.Name = name;

            var mustBePrivate = await IsInPrivateTreeAsync(folderId);

            folder.Acssesability = mustBePrivate
                ? Acssesability.Private
                : acssesability;

            await context.SaveChangesAsync();

            if (folder.Acssesability == Acssesability.Private)
            {
                await ApplyPrivateToChildrenAsync(folder.Id);
            }
        }

        private async Task<bool> IsInPrivateTreeAsync(Guid folderId)
        {
            var currentFolderId = folderId;

            while (true)
            {
                var currentFolder = await context.Folders
                    .FirstOrDefaultAsync(f => f.Id == currentFolderId);

                if (currentFolder == null)
                {
                    return false;
                }

                if (currentFolder.Acssesability == Acssesability.Private)
                {
                    return true;
                }

                var parentLink = await context.FolderStructures
                    .FirstOrDefaultAsync(fs => fs.SubfolderId == currentFolderId);

                if (parentLink == null)
                {
                    return false;
                }

                currentFolderId = parentLink.FolderId;
            }
        }

        private async Task ApplyPrivateToChildrenAsync(Guid folderId)
        {
            var childFolderLinks = await context.FolderStructures
                .Where(fs => fs.FolderId == folderId)
                .ToListAsync();

            var childFolderIds = childFolderLinks
                .Select(fs => fs.SubfolderId)
                .ToList();

            if (childFolderIds.Any())
            {
                var childFolders = await context.Folders
                    .Where(f => childFolderIds.Contains(f.Id))
                    .ToListAsync();

                foreach (var childFolder in childFolders)
                {
                    childFolder.Acssesability = Acssesability.Private;
                }
            }

            var pictures = await context.Pictures
                .Where(p => p.FolderId == folderId)
                .ToListAsync();

            foreach (var picture in pictures)
            {
                picture.Acssesability = Acssesability.Private;
            }

            await context.SaveChangesAsync();

            foreach (var childFolderId in childFolderIds)
            {
                await ApplyPrivateToChildrenAsync(childFolderId);
            }
        }
    }
}
