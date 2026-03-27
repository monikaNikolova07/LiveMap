namespace LiveMap.Web.Models.Home
{
    public class ExplorePictureViewModel
    {
        public Guid PictureId { get; set; }
        public Guid FolderId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public Guid ProfileId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string ProfilePicture { get; set; } = string.Empty;
        public string FolderName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedOn { get; set; }
        public int FollowersCount { get; set; }
        public int LikesCount { get; set; }
        public int CommentsCount { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
        public bool CanInteract { get; set; }
        public List<ExploreCommentViewModel> Comments { get; set; } = new();
    }

    public class ExploreCommentViewModel
    {
        public string Username { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
    }
}
