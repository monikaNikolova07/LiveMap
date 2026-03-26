namespace LiveMap.Web.Models.Profile
{
    public class ProfileFolderViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string AccessibilityLabel { get; set; } = string.Empty;
    }
}
