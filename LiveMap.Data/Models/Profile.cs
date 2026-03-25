using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LiveMap.Data.Models
{
    public class Profile
    {
        [Key]
        public Guid Id { get; set; }

        public string? ProfilePicture { get; set; }

        public string? Bio { get; set; }

        public Acssesability Acssesability { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        public Guid UserId { get; set; }

        public List<Folder> Folders { get; set; } = new();
    }
}
