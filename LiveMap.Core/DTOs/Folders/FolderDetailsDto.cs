using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveMap.Core.DTOs.Folders
{
    public class FolderDetailsDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public List<string> PicturesUrls { get; set; }
    }
}
