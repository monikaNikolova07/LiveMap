using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace LiveMap.Data.Models
{
    [PrimaryKey(nameof(FolderId), nameof(SubfolderId))]
    public class FolderStructure
    {
        [ForeignKey(nameof(FolderId))]
        public Folder Folder { get; set; } = null!;

        public Guid FolderId { get; set; }

        [ForeignKey(nameof(SubfolderId))]
        public Folder Subfolder { get; set; } = null!;

        public Guid SubfolderId { get; set; }
    }
}
