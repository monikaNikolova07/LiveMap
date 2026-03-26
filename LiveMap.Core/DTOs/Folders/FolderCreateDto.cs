using LiveMap.Data.Models;
using System.ComponentModel.DataAnnotations;

namespace LiveMap.Core.DTOs.Folders
{
    public class FolderCreateDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public Acssesability Acssesability { get; set; } = Acssesability.Public;

        public Guid? ParentFolderId { get; set; }

        public bool IsCountryFolder { get; set; } = true;
    }
}
