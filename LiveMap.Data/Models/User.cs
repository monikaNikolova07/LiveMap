using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveMap.Data.Models
{
    public class User : IdentityUser<Guid>
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public Profile? Profile { get; set; }

        [InverseProperty(nameof(UserFollowing.User))]
        public ICollection<UserFollowing>? Followings { get; set; }

        [InverseProperty(nameof(UserFollowing.Following))]
        public ICollection<UserFollowing>? Followers { get; set; }
    }
}
