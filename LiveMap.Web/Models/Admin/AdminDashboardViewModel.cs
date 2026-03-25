using LiveMap.Core.DTOs.Admin;

namespace LiveMap.Web.Models.Admin
{
    public class AdminDashboardViewModel
    {
        public List<AdminProfileItemDto> Profiles { get; set; } = new();
        public List<AdminFolderItemDto> Folders { get; set; } = new();
        public List<AdminPhotoItemDto> Photos { get; set; } = new();
    }
}
