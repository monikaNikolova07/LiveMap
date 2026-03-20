using LiveMap.Core.DTOs.Folders;
using LiveMap.Data.Models;
using Microsoft.AspNetCore.Http;

namespace LiveMap.Core.Contracts
{
    public interface IFolderService
    {
        Task<IEnumerable<FolderIndexDto>> GetAllAsync();

        Task CreateAsync(FolderCreateDto model, Guid userId);

        Task<Folder?> GetByIdAsync(Guid id);

        Task<IEnumerable<Picture>> GetPicturesAsync(Guid folderId);

        Task UploadPictureAsync(Guid folderId, IFormFile file);

        Task DeleteAsync(Guid id);
    }
}
