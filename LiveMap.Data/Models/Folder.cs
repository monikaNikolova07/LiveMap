using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LiveMap.Data.Models
{
    public class Folder
    {
        [Key]
        public Guid Id { get; set; }

        public string Name { get; set; } = null!;

        [ForeignKey(nameof(ProfileId))]
        public Profile Profile { get; set; } = null!;

        public Guid ProfileId { get; set; }

        public List<Picture> Pictures { get; set; } = new();

        [InverseProperty(nameof(FolderStructure.Folder))]
        public ICollection<FolderStructure> Subfolders { get; set; } = new List<FolderStructure>();

        [InverseProperty(nameof(FolderStructure.Subfolder))]
        public ICollection<FolderStructure> ParentFolders { get; set; } = new List<FolderStructure>();

        public Acssesability Acssesability { get; set; }
    }
}
