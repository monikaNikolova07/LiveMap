using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace LiveMap.Data.Models
{
    [PrimaryKey(nameof(UserId), nameof(FolowingId))]
    public class UserFollowing
    {
        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        public Guid UserId { get; set; }

        [ForeignKey(nameof(FolowingId))]
        public User Following { get; set; } = null!;

        public Guid FolowingId { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    }
}
