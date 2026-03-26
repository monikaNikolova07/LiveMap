namespace LiveMap.Web.Models.Profile
{
    public class ProfileConnectionItemViewModel
    {
        public Guid ProfileId { get; set; }
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? ProfilePicture { get; set; }
        public string? Bio { get; set; }
    }
}
