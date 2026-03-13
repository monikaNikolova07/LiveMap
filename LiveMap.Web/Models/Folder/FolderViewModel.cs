using LiveMap.Data.Models;

namespace LiveMap.Models.Folder
{
    public class FolderViewModel
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public Guid ProfileId { get; set; }

        public string ProfilePicture { get; set; }

        public int PicturesCount { get; set; }

        public Acssesability Acssesability { get; set; }
    }
}
