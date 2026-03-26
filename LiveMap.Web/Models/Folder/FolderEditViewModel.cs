using LiveMap.Data.Models;
using System.ComponentModel.DataAnnotations;

namespace LiveMap.Web.Models.Folder
{
    public class FolderEditViewModel
    {
        [Required]
        public Guid Id { get; set; }

        public Guid? ParentFolderId { get; set; }

        public Guid ProfileId { get; set; }

        public bool IsCountryFolder { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public Acssesability Acssesability { get; set; }
    }
}
