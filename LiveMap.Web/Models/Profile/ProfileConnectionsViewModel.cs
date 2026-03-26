namespace LiveMap.Web.Models.Profile
{
    public class ProfileConnectionsViewModel
    {
        public Guid ProfileId { get; set; }
        public string ProfileUsername { get; set; } = string.Empty;
        public string ConnectionType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public ICollection<ProfileConnectionItemViewModel> Items { get; set; } = new List<ProfileConnectionItemViewModel>();
    }
}
