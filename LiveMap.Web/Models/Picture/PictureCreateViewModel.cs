using LiveMap.Data.Models;
using System.ComponentModel.DataAnnotations;

namespace LiveMap.Models.Picture
{
    public class PictureCreateViewModel
    {
        [Required]
        public string URL { get; set; }

        [Required]
        public Guid FolderId { get; set; }

        public Acssesability Acssesability { get; set; }
    }
}
