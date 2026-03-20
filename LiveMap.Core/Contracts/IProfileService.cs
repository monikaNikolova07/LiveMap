using LiveMap.Core.DTOs.Profiles;

namespace LiveMap.Core.Contracts
{
    public interface IProfileService
    {
        Task<ProfileIndexDto?> GetProfileAsync(string userId);
        Task<ProfileEditDto?> GetProfileForEditAsync(string userId);

        Task EditProfileAsync(ProfileEditDto dto, string userId);
    }
}
