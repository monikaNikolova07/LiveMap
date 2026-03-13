using LiveMap.Data;
using LiveMap.Models.Profile;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LiveMap.Controllers
{
    public class ProfileController : Controller
    {
        private readonly LiveMapDbContext context;

        public ProfileController(LiveMapDbContext context)
        {
            this.context = context;
        }

        public async Task<IActionResult> Index()
        {
            var profiles = await context.Profiles
                .Select(p => new ProfileViewModel
                {
                    Id = p.Id,
                    ProfilePicture = p.ProfilePicture,
                    Bio = p.Bio,
                    UserId = p.UserId,
                    FoldersCount = p.Folders.Count,
                    Acssesability = p.Acssesability
                })
                .ToListAsync();

            return View(profiles);
        }
    }
}
