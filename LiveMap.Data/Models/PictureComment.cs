using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LiveMap.Data.Models
{
    public class PictureComment
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(500)]
        public string Content { get; set; } = null!;

        public Guid PictureId { get; set; }

        [ForeignKey(nameof(PictureId))]
        public Picture Picture { get; set; } = null!;

        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    }
}
