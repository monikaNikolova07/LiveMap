using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveMap.Data.Models
{
    [PrimaryKey(nameof(UserId), nameof(FolowingId))]
    public class UserFollowing
    {
        [ForeignKey(nameof(UserId))]
        public User User { get; set; }
        public Guid UserId { get; set; }

        [ForeignKey(nameof(FolowingId))]
        public User Following { get; set; }
        public Guid FolowingId { get; set; }
    }
}
