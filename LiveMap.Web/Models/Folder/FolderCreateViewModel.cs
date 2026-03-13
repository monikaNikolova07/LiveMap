using LiveMap.Data.Models;
using System.ComponentModel.DataAnnotations;

namespace LiveMap.Models.Folder
{
    public class FolderCreateViewModel
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public Guid ProfileId { get; set; }

        public Acssesability Acssesability { get; set; }
    }
}
