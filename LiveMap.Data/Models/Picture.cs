using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LiveMap.Data.Models
{
    public class Picture
    {
        [Key]
        public Guid Id { get; set; }

        public string URL { get; set; } = null!;

        [ForeignKey(nameof(FolderId))]
        public Folder Folder { get; set; } = null!;

        public Guid FolderId { get; set; }

        public Acssesability Acssesability { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public ICollection<PictureLike> Likes { get; set; } = new List<PictureLike>();

        public ICollection<PictureComment> Comments { get; set; } = new List<PictureComment>();
    }
}
