using LiveMap.Core.DTOs.Folders;
using LiveMap.Data.Models;
using Microsoft.AspNetCore.Http;

namespace LiveMap.Core.Contracts
{
    public interface IFolderService
    {
        Task<IEnumerable<FolderIndexDto>> GetAllAsync(Guid? currentUserId = null);

        Task<Folder> CreateAsync(FolderCreateDto model, Guid userId);

        Task<Folder?> GetByIdAsync(Guid id);

        Task<IEnumerable<Picture>> GetPicturesAsync(Guid folderId);

        Task<IEnumerable<Acssesability>> GetAvailableAccessibilitiesForCreateAsync(Guid? parentFolderId);

        Task<Acssesability> GetUploadAccessibilityAsync(Guid folderId, Acssesability requestedAccessibility);

        Task UploadPictureAsync(Guid folderId, IFormFile file, Acssesability acssesability, string? description);

        Task DeleteAsync(Guid id); 
        Task UpdateAsync(Guid folderId, string name, Acssesability acssesability);
    }
}
