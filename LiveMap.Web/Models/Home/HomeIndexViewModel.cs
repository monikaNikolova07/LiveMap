namespace LiveMap.Web.Models.Home;

public class HomeIndexViewModel
{
    public List<CountryOptionViewModel> Countries { get; set; } = [];
    public Dictionary<string, string> CountryAliases { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
