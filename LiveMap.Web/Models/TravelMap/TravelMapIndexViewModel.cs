using System.Text.Json.Serialization;

namespace LiveMap.Web.Models.TravelMap
{
    public class TravelMapIndexViewModel
    {
        public string DefaultColor { get; set; } = "#facc15";

        public List<string> Palette { get; set; } = new();

        public List<TravelMapCountryViewModel> VisitedCountries { get; set; } = new();

        public Dictionary<string, string> CountryAliases { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        [JsonIgnore]
        public string Title => "Passport Palette";
    }
}
