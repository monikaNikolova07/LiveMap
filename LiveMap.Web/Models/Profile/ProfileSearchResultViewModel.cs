namespace LiveMap.Web.Models.Profile
{
    public class ProfileSearchResultViewModel
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? ProfilePicture { get; set; }
        public string? Bio { get; set; }
        public int FollowersCount { get; set; }
        public bool IsOwnProfile { get; set; }
        public bool IsFollowedByCurrentUser { get; set; }
    }
}
