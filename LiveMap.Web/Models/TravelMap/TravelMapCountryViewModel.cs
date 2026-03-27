namespace LiveMap.Web.Models.TravelMap
{
    public class TravelMapCountryViewModel
    {
        public string CountryValue { get; set; } = string.Empty;

        public string CountryName { get; set; } = string.Empty;

        public string ColorHex { get; set; } = "#facc15";

        public int FolderCount { get; set; }

        public int PictureCount { get; set; }
    }
}
