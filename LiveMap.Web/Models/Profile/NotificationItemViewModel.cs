namespace LiveMap.Web.Models.Profile
{
    public class NotificationItemViewModel
    {
        public Guid ProfileId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? ProfilePicture { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}
