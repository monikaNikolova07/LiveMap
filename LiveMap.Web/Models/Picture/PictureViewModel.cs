using LiveMap.Data.Models;

namespace LiveMap.Web.Models.Picture
{
    public class PictureViewModel
    {
        public Guid Id { get; set; }

        public string URL { get; set; }

        public Guid FolderId { get; set; }

        public string FolderName { get; set; }

        public Acssesability Acssesability { get; set; }
        public int LikesCount { get; set; }
        public int CommentsCount { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
    }
}
