using LiveMap.Core.DTOs.Admin;

namespace LiveMap.Core.Contracts
{
    public interface IAdminService
    {
        Task<AdminDashboardDto> GetDashboardAsync();
        Task DeleteProfileAsync(Guid profileId);
        Task DeleteFolderAsync(Guid folderId);
        Task DeletePhotoAsync(Guid photoId);
        Task DeleteProfilePictureAsync(Guid profileId);
        Task EnsureRolesAndAdminsAsync(IEnumerable<string> adminEmails);
    }
}
