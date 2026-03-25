namespace LiveMap.Web.Models.Profile
{
    public class ProfileSearchPageViewModel
    {
        public string Term { get; set; } = string.Empty;
        public ICollection<ProfileSearchResultViewModel> Results { get; set; } = new List<ProfileSearchResultViewModel>();
    }
}
