using LiveMap.Data;
using LiveMap.Web.Models.Profile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LiveMap.Web.Controllers
{
    [Authorize]
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

        /* za VIew na Create /nqmam metod oshte/
         * @model LiveMap.Web.Models.Profile.ProfileCreateViewModel

            <h2>Create Profile</h2>

            <form asp-action="Create">
              <div>
              <label>Username</label>
              <input asp-for="Username" />
             </div>

             <div>
                 <label>Bio</label>
                 <textarea asp-for="Bio"></textarea>
            </div>

             <button type="submit">Create</button>
            </form>
         */
    }
}
