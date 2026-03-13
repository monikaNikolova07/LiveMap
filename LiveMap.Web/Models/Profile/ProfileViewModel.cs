using LiveMap.Data.Models;

namespace LiveMap.Models.Profile
{
    public class ProfileViewModel
    {
        public Guid Id { get; set; }

        public string ProfilePicture { get; set; }

        public string Bio { get; set; }

        public Guid UserId { get; set; }

        public int FoldersCount { get; set; }

        public Acssesability Acssesability { get; set; }
    }
}
