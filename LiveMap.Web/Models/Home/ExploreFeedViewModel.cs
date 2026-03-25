namespace LiveMap.Web.Models.Home
{
    public class ExploreFeedViewModel
    {
        public ICollection<ExplorePictureViewModel> Pictures { get; set; } = new List<ExplorePictureViewModel>();
    }
}
