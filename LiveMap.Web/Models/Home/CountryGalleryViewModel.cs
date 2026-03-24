namespace LiveMap.Web.Models.Home;

public class CountryGalleryViewModel
{
    public string CountryValue { get; set; } = string.Empty;
    public string CountryName { get; set; } = string.Empty;
    public string CountryFlagEmoji { get; set; } = "🌍";
    public List<CountryImageCardViewModel> Images { get; set; } = [];
}
