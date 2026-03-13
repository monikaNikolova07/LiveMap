using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LiveMap.Data.Models
{
    public class Folder
    {
        [Key]
        public Guid Id { get; set; }
        public string Name { get; set; }

        [ForeignKey(nameof(ProfileId))]
        public Profile Profile { get; set; }
        public Guid ProfileId { get; set; }

        public List<Picture> Pictures { get; set; }

        [InverseProperty(nameof(FolderStructure.Folder))]
        public ICollection<FolderStructure> Subfolders { get; set; }

        [InverseProperty(nameof(FolderStructure.Subfolder))]
        public ICollection<FolderStructure> ParentFolders { get; set; }

        public Acssesability Acssesability { get; set; }
    }
}
