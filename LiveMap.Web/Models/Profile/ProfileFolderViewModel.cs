namespace LiveMap.Web.Models.Profile
{
    public class ProfileFolderViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string AccessibilityLabel { get; set; } = string.Empty;
        public bool IsCountryFolder { get; set; }
        public string FlagEmoji { get; set; } = "📁";
        public string FlagPaletteStyle { get; set; } = string.Empty;
    }
}
