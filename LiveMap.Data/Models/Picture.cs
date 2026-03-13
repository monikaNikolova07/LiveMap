using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveMap.Data.Models
{
    public class Picture
    {
        [Key]
        public Guid Id { get; set; }
        public string URL { get; set; }

        [ForeignKey(nameof(FolderId))]
        public Folder Folder { get; set; }
        public Guid FolderId { get; set; }

        public Acssesability Acssesability { get; set; }
    }
}
