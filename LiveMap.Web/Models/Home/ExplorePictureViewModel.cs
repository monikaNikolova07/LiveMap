namespace LiveMap.Web.Models.Home
{
    public class ExplorePictureViewModel
    {
        public Guid PictureId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public Guid ProfileId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string ProfilePicture { get; set; } = string.Empty;
        public string FolderName { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
        public int FollowersCount { get; set; }
    }
}
