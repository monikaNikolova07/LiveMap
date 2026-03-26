using LiveMap.Data.Models;
using System.ComponentModel.DataAnnotations;

namespace LiveMap.Web.Models.Picture
{
    public class PictureEditVisibilityViewModel
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        public Guid FolderId { get; set; }

        public string Url { get; set; } = string.Empty;

        [Required]
        public Acssesability Acssesability { get; set; }
    }
}
