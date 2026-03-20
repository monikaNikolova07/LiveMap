using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveMap.Core.DTOs.Profiles
{
    public class ProfileEditDto
    {
        public Guid Id { get; set; }

        public string Bio { get; set; } = null!;

        public string ProfilePicture { get; set; } = null!;

        public int Acssesability { get; set; }
    }
}
