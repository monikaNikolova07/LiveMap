using System.ComponentModel.DataAnnotations.Schema;

namespace LiveMap.Data.Models
{
    public class PictureLike
    {
        public Guid PictureId { get; set; }

        [ForeignKey(nameof(PictureId))]
        public Picture Picture { get; set; } = null!;

        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    }
}
