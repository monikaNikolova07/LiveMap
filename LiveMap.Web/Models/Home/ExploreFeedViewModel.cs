namespace LiveMap.Web.Models.Home
{
    public class ExploreFeedViewModel
    {
        public ICollection<ExplorePictureViewModel> Pictures { get; set; } = new List<ExplorePictureViewModel>();

        public string SelectedFilter { get; set; } = ExploreFilterOptions.Newest;

        public string UsernameQuery { get; set; } = string.Empty;

        public string Heading { get; set; } = "Newest public pictures";

        public string Description { get; set; } = "Browse the latest uploaded public images and open the creator profiles.";

        public bool RequiresLoginNotice { get; set; }

        public int ResultsCount => Pictures.Count;
    }

    public static class ExploreFilterOptions
    {
        public const string Newest = "newest";
        public const string Oldest = "oldest";
        public const string ByUser = "user";
        public const string PopularUsers = "popular-users";
        public const string FollowingNewest = "following";
        public const string FriendsNewest = "friends";
    }
}
