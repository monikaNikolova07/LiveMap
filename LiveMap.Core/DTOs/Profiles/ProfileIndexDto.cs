using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveMap.Core.DTOs.Profiles
{
    public class ProfileIndexDto
    {
        public Guid Id { get; set; }

        public string ProfilePicture { get; set; } = null!;

        public string Bio { get; set; } = null!;

        public string Username { get; set; } = null!;

        public ICollection<ProfileFolderDto> Folders { get; set; }
            = new List<ProfileFolderDto>();
    }
}
