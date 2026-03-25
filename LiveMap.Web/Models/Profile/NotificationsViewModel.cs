namespace LiveMap.Web.Models.Profile
{
    public class NotificationsViewModel
    {
        public ICollection<NotificationItemViewModel> Followers { get; set; } = new List<NotificationItemViewModel>();
    }
}
