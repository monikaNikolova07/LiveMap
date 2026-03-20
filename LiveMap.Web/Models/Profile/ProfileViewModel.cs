using LiveMap.Data.Models;

namespace LiveMap.Web.Models.Profile
{
    public class ProfileViewModel
    {
        public Guid Id { get; set; }

        public string ProfilePicture { get; set; } = null!;

        public string Bio { get; set; } = null!;

        public string Username { get; set; } = null!;

        public ICollection<ProfileFolderViewModel> Folders { get; set; }
            = new List<ProfileFolderViewModel>();
    }
}
