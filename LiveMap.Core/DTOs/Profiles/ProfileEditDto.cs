using System.ComponentModel.DataAnnotations;

namespace LiveMap.Core.DTOs.Profiles
{
    public class ProfileEditDto
    {
        public Guid Id { get; set; }

        [Required]
        [StringLength(30, MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;

        [StringLength(500)]
        public string Bio { get; set; } = string.Empty;

        public string ProfilePicture { get; set; } = string.Empty;
    }
}
