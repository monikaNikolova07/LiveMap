using LiveMap.Data.Models;

namespace LiveMap.Web.Models.Profile
{
    public class ProfileViewModel
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public string ProfilePicture { get; set; } = string.Empty;

        public string Bio { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        public int FollowersCount { get; set; }

        public int FollowingCount { get; set; }

        public int FriendsCount { get; set; }

        public bool IsOwnProfile { get; set; }

        public bool IsFollowedByCurrentUser { get; set; }

        public ICollection<ProfileFolderViewModel> Folders { get; set; }
            = new List<ProfileFolderViewModel>();
    }
}
