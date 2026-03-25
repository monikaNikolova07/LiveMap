using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace LiveMap.Data.Models
{
    public class User : IdentityUser<Guid>
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public Profile? Profile { get; set; }

        [InverseProperty(nameof(UserFollowing.User))]
        public ICollection<UserFollowing> Followings { get; set; } = new List<UserFollowing>();

        [InverseProperty(nameof(UserFollowing.Following))]
        public ICollection<UserFollowing> Followers { get; set; } = new List<UserFollowing>();
    }
}
